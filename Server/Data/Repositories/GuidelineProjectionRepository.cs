// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Domain.IRepositories;
using InstanceService.Models.Guideline;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InstanceService.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGuidelineProjectionRepository"/>.
/// Uses a PostgreSQL transaction-scoped advisory lock per ServiceId so concurrent consumers cannot
/// interleave upserts of the same guideline, and an ETag idempotency guard so re-delivered events are
/// no-ops. Mirrors the AccessService projection but tracks removed IDs for graph-instance cleanup
/// rather than access-right cleanup.
/// </summary>
public class GuidelineProjectionRepository : IGuidelineProjectionRepository
{
    private readonly InstanceServiceDbContext _context;

    public GuidelineProjectionRepository(InstanceServiceDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, string etag, CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineVersions
            .AnyAsync(g => g.Id == id && g.Etag == etag, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuidelineUpsertResult> UpsertAsync(GuidelineVersion newVersion, CancellationToken cancellationToken = default)
    {
        var lockKey = ComputeAdvisoryLockId(newVersion.Id.ToString());

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            var existing = await FindExistingAsync(newVersion, cancellationToken);

            // Idempotency: same identity + same etag = already fully processed
            if (existing != null && existing.Etag == newVersion.Etag)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new GuidelineUpsertResult([], []);
            }

            GuidelineUpsertResult result;
            if (existing == null)
            {
                await _context.GuidelineVersions.AddAsync(newVersion, cancellationToken);
                result = new GuidelineUpsertResult([], []);
            }
            else
            {
                UpdateVersionScalars(existing, newVersion);
                var (removedClassIds, removedPropIds) = ApplyClassifications(existing, newVersion.Classifications);
                ApplyPropertySets(existing, newVersion.PropertySets);
                ApplyProperties(existing, newVersion.Properties);
                result = new GuidelineUpsertResult(removedClassIds, removedPropIds);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Concurrent consumer already processed the same version — treat as success
            await transaction.RollbackAsync(cancellationToken);
            return new GuidelineUpsertResult([], []);
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetClassificationIdsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineVersions
            .Where(v => v.Id == id)
            .SelectMany(v => v.Classifications)
            .Select(c => c.ClassificationId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.GuidelineVersions
            .Where(g => g.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetClassificationNamesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.GuidelineClassifications
            .AsNoTracking()
            .Select(c => new { c.ClassificationId, c.Name })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, string>(rows.Count);
        foreach (var r in rows)
            map[r.ClassificationId] = r.Name;
        return map;
    }

    /// <inheritdoc/>
    public async Task<List<ClassificationSummary>> GetClassificationSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineClassifications
            .AsNoTracking()
            .Select(c => new ClassificationSummary(c.ClassificationId, c.Name, c.GuidelineVersion.Name))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ClassificationGraph?> GetClassificationWithReferencesAsync(string classificationId, CancellationToken cancellationToken = default)
    {
        var cls = await _context.GuidelineClassifications
            .AsNoTracking()
            .Include(c => c.ClassificationProperties)
            .FirstOrDefaultAsync(c => c.ClassificationId == classificationId, cancellationToken);

        if (cls == null)
            return null;

        var versionId = cls.GuidelineVersionId;
        var propIds = cls.ClassificationProperties.Select(cp => cp.PropertyId).Distinct().ToList();
        var setIds = cls.ClassificationProperties
            .Where(cp => cp.PropertySetId != null)
            .Select(cp => cp.PropertySetId!)
            .Distinct()
            .ToList();

        var props = await _context.GuidelineProperties
            .AsNoTracking()
            .Where(p => p.GuidelineVersionId == versionId && propIds.Contains(p.PropertyId))
            .ToListAsync(cancellationToken);

        var sets = setIds.Count > 0
            ? await _context.GuidelinePropertySets
                .AsNoTracking()
                .Where(s => s.GuidelineVersionId == versionId && setIds.Contains(s.PropertySetId))
                .ToListAsync(cancellationToken)
            : [];

        return new ClassificationGraph(cls, props, sets);
    }

    /// <inheritdoc/>
    public async Task<List<GuidelineVersion>> GetAllVersionsWithChildrenAsync(CancellationToken cancellationToken = default)
    {
        // AsSplitQuery avoids the cartesian explosion of joining all child collections in one query.
        return await _context.GuidelineVersions
            .AsNoTracking()
            .Include(v => v.Classifications).ThenInclude(c => c.ClassificationProperties)
            .Include(v => v.PropertySets)
            .Include(v => v.Properties)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<GuidelineVersion?> FindExistingAsync(GuidelineVersion newVersion, CancellationToken ct)
    {
        // AsSplitQuery avoids the cartesian explosion that a single JOIN across
        // Classifications × ClassificationProperties × PropertySets × Properties would produce.
        return await _context.GuidelineVersions
            .Include(v => v.Classifications).ThenInclude(c => c.ClassificationProperties)
            .Include(v => v.PropertySets)
            .Include(v => v.Properties)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == newVersion.Id, ct);
    }

    private static void UpdateVersionScalars(GuidelineVersion existing, GuidelineVersion src)
    {
        existing.GuidelineId = src.GuidelineId;
        existing.Name = src.Name;
        existing.Identifier = src.Identifier;
        existing.Description = src.Description;
        existing.Version = src.Version;
        existing.ObjectName = src.ObjectName;
        existing.BucketName = src.BucketName;
        existing.Etag = src.Etag;
        existing.CorrelationId = src.CorrelationId;
        existing.EventTimestamp = src.EventTimestamp;
        existing.ProcessedAt = src.ProcessedAt;
        existing.MappingsJson = src.MappingsJson;
        existing.ComplexDataJson = src.ComplexDataJson;
        existing.DomainJson = src.DomainJson;
    }

    private (List<string> removedClassIds, List<string> removedPropIds) ApplyClassifications(
        GuidelineVersion existing, ICollection<GuidelineClassification> incoming)
    {
        var existingByKey = existing.Classifications.ToDictionary(c => c.ClassificationId);
        var incomingKeys = new HashSet<string>(incoming.Select(c => c.ClassificationId));
        var removedClassIds = new List<string>();
        var removedPropIds = new List<string>();

        // Remove classifications no longer in the guideline; their instances become orphaned
        foreach (var cls in existing.Classifications.Where(c => !incomingKeys.Contains(c.ClassificationId)).ToList())
        {
            _context.GuidelineClassifications.Remove(cls);
            removedClassIds.Add(cls.ClassificationId);
        }

        foreach (var newCls in incoming)
        {
            if (existingByKey.TryGetValue(newCls.ClassificationId, out var existingCls))
            {
                existingCls.Name = newCls.Name;
                existingCls.Identifier = newCls.Identifier;
                existingCls.Code = newCls.Code;
                existingCls.Description = newCls.Description;
                existingCls.Status = newCls.Status;
                existingCls.RelationsJson = newCls.RelationsJson;

                var removed = ApplyClassificationProperties(existingCls, newCls.ClassificationProperties);
                removedPropIds.AddRange(removed);
            }
            else
            {
                newCls.GuidelineVersionId = existing.Id;
                _context.GuidelineClassifications.Add(newCls);
            }
        }

        return (removedClassIds, removedPropIds);
    }

    private List<string> ApplyClassificationProperties(
        GuidelineClassification existingCls, ICollection<GuidelineClassificationProperty> incoming)
    {
        var existingByKey = existingCls.ClassificationProperties.ToDictionary(cp => cp.ClassificationPropertyId);
        var incomingKeys = new HashSet<string>(incoming.Select(cp => cp.ClassificationPropertyId));
        var removedPropIds = new List<string>();

        foreach (var cp in existingCls.ClassificationProperties.Where(cp => !incomingKeys.Contains(cp.ClassificationPropertyId)).ToList())
        {
            _context.GuidelineClassificationProperties.Remove(cp);
            removedPropIds.Add(cp.ClassificationPropertyId);
        }

        foreach (var newCp in incoming)
        {
            if (existingByKey.TryGetValue(newCp.ClassificationPropertyId, out var existingCp))
            {
                existingCp.PropertyId = newCp.PropertyId;
                existingCp.PropertySetId = newCp.PropertySetId;
                existingCp.IsRequired = newCp.IsRequired;
                existingCp.SortNumber = newCp.SortNumber;
                existingCp.IsReadonly = newCp.IsReadonly;
                existingCp.DefaultValue = newCp.DefaultValue;
                existingCp.Reference = newCp.Reference;
                existingCp.AssignmentJson = newCp.AssignmentJson;
            }
            else
            {
                newCp.GuidelineClassificationId = existingCls.Id;
                _context.GuidelineClassificationProperties.Add(newCp);
            }
        }

        return removedPropIds;
    }

    private void ApplyPropertySets(GuidelineVersion existing, ICollection<GuidelinePropertySet> incoming)
    {
        var existingByKey = existing.PropertySets.ToDictionary(ps => ps.PropertySetId);
        var incomingKeys = new HashSet<string>(incoming.Select(ps => ps.PropertySetId));

        foreach (var ps in existing.PropertySets.Where(ps => !incomingKeys.Contains(ps.PropertySetId)).ToList())
            _context.GuidelinePropertySets.Remove(ps);

        foreach (var newPs in incoming)
        {
            if (existingByKey.TryGetValue(newPs.PropertySetId, out var existingPs))
            {
                existingPs.Name = newPs.Name;
                existingPs.Identifier = newPs.Identifier;
                existingPs.Description = newPs.Description;
                existingPs.Status = newPs.Status;
            }
            else
            {
                newPs.GuidelineVersionId = existing.Id;
                _context.GuidelinePropertySets.Add(newPs);
            }
        }
    }

    private void ApplyProperties(GuidelineVersion existing, ICollection<GuidelineProperty> incoming)
    {
        var existingByKey = existing.Properties.ToDictionary(p => p.PropertyId);
        var incomingKeys = new HashSet<string>(incoming.Select(p => p.PropertyId));

        foreach (var p in existing.Properties.Where(p => !incomingKeys.Contains(p.PropertyId)).ToList())
            _context.GuidelineProperties.Remove(p);

        foreach (var newP in incoming)
        {
            if (existingByKey.TryGetValue(newP.PropertyId, out var existingP))
            {
                existingP.Name = newP.Name;
                existingP.Identifier = newP.Identifier;
                existingP.Description = newP.Description;
                existingP.StorageType = newP.StorageType;
                existingP.Code = newP.Code;
                existingP.UnitType = newP.UnitType;
                existingP.UnitAbbreviation = newP.UnitAbbreviation;
                existingP.Status = newP.Status;
                existingP.PropertyType = newP.PropertyType;
                existingP.ExtraJson = newP.ExtraJson;
            }
            else
            {
                newP.GuidelineVersionId = existing.Id;
                _context.GuidelineProperties.Add(newP);
            }
        }
    }

    private static long ComputeAdvisoryLockId(string key)
    {
        return BitConverter.ToInt64(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(key)).AsSpan(0, 8));
    }
}
