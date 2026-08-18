// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Enums;
using InstanceService.Domain.IRepositories;
using InstanceService.Api.Serialization;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using ClassificationSummary = InstanceService.Domain.IRepositories.ClassificationSummary;

using Entities = InstanceService.Models.Guideline;
using GuidelineModel = Guideline.Model.Model;

namespace InstanceService.Api.Services;

/// <summary>
/// Reconstructs the full <c>Guideline.Model</c> object graph from the relational guideline projection
/// (the tables filled by <see cref="GuidelineTransformationService"/>). All stored guidelines are merged
/// into a single <see cref="GuidelineModel.IGuideline"/>; callers then reduce it per use case via access rights.
/// The reconstructed graph is cached in memory and invalidated when the projection changes
/// (see <see cref="Invalidate"/>), mirroring the previous MinIO-download caching behaviour.
/// </summary>
public interface IGuidelineReconstructionService
{
    /// <summary>Builds (or returns the cached) full guideline reconstructed from all stored guidelines.</summary>
    Task<GuidelineModel.IGuideline> GetFullGuidelineAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a cached, lightweight classification-id → name map. Cheap — for hot paths (e.g. mapping
    /// instances to DTOs) that only need the display name and must NOT trigger a full reconstruction.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetClassificationNamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a cached lightweight list of classification summaries (id, name, guideline name).
    /// Used to populate the classification picker with enough context to disambiguate same-named entries.
    /// </summary>
    Task<IReadOnlyList<ClassificationSummary>> GetClassificationSummariesAsync(CancellationToken ct = default);

    /// <summary>
    /// Reconstructs a single classification (with its classification properties, their property references and
    /// property sets) by classification id, without materializing the whole guideline. Cached. Returns
    /// <c>null</c> if the classification is not present in any stored guideline.
    /// </summary>
    Task<GuidelineModel.IClassification?> GetClassificationAsync(string classificationId, CancellationToken ct = default);

    /// <summary>Drops the cached reconstruction so the next call rebuilds it from the database.</summary>
    void Invalidate();

    /// <summary>
    /// Monotonically increasing token that changes whenever the guideline projection is invalidated.
    /// Downstream caches (e.g. the per-use-case reduced guideline) include it in their key so a guideline
    /// change takes effect immediately rather than waiting for a TTL.
    /// </summary>
    long Generation { get; }
}

/// <inheritdoc/>
public class GuidelineReconstructionService(
    IGuidelineProjectionRepository repository,
    IMemoryCache cache,
    ILogger<GuidelineReconstructionService> logger) : IGuidelineReconstructionService
{
    private const string CacheKey = "FULL_GUIDELINE_RECONSTRUCTION";
    private const string NamesCacheKey = "GUIDELINE_CLASSIFICATION_NAMES";
    private const string SummariesCacheKey = "GUIDELINE_CLASSIFICATION_SUMMARIES";

    // Process-wide so it survives across scoped instances of this service.
    private static long _generation;

    /// <inheritdoc/>
    public async Task<GuidelineModel.IGuideline> GetFullGuidelineAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out GuidelineModel.IGuideline? cached) && cached is not null)
            return cached;

        var versions = await repository.GetAllVersionsWithChildrenAsync(ct);
        var guideline = BuildMergedGuideline(versions);

        cache.Set(CacheKey, guideline, new MemoryCacheEntryOptions { Size = 1024 });
        logger.LogInformation(
            "Reconstructed full guideline from {VersionCount} stored guideline(s): {ClassCount} classifications, {PropCount} properties.",
            versions.Count,
            guideline.Domain?.Classifications?.Count ?? 0,
            guideline.Domain?.Properties?.Count ?? 0);

        return guideline;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, string>> GetClassificationNamesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(NamesCacheKey, out IReadOnlyDictionary<string, string>? cached) && cached is not null)
            return cached;

        var names = await repository.GetClassificationNamesAsync(ct);
        cache.Set(NamesCacheKey, (IReadOnlyDictionary<string, string>)names, new MemoryCacheEntryOptions { Size = 1024 });
        return names;
    }

    /// <inheritdoc/>
    public async Task<GuidelineModel.IClassification?> GetClassificationAsync(string classificationId, CancellationToken ct = default)
    {
        var key = $"GUIDELINE_CLASSIFICATION::{Generation}::{classificationId}";
        if (cache.TryGetValue(key, out GuidelineModel.IClassification? cached) && cached is not null)
            return cached;

        var data = await repository.GetClassificationWithReferencesAsync(classificationId, ct);
        if (data is null)
            return null;

        var propsById = new Dictionary<string, Entities.GuidelineProperty>();
        foreach (var p in data.Properties)
            propsById[p.PropertyId] = p;

        var setsById = new Dictionary<string, Entities.GuidelinePropertySet>();
        foreach (var s in data.PropertySets)
            setsById[s.PropertySetId] = s;

        GuidelineModel.IClassification result = ReconstructClassification(data.Classification, propsById, setsById);

        cache.Set(key, result, new MemoryCacheEntryOptions
        {
            Size = 1024,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        });
        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClassificationSummary>> GetClassificationSummariesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(SummariesCacheKey, out IReadOnlyList<ClassificationSummary>? cached) && cached is not null)
            return cached;

        var summaries = await repository.GetClassificationSummariesAsync(ct);
        cache.Set(SummariesCacheKey, (IReadOnlyList<ClassificationSummary>)summaries, new MemoryCacheEntryOptions { Size = 1024 });
        return summaries;
    }

    /// <inheritdoc/>
    public long Generation => Interlocked.Read(ref _generation);

    /// <inheritdoc/>
    public void Invalidate()
    {
        cache.Remove(CacheKey);
        cache.Remove(NamesCacheKey);
        cache.Remove(SummariesCacheKey);
        Interlocked.Increment(ref _generation);
    }

    // ── Reconstruction (ported from AccessService.UseCaseGuidelineService) ───────

    private static GuidelineModel.IGuideline BuildMergedGuideline(List<Entities.GuidelineVersion> versions)
    {
        var allClassifications = new List<GuidelineModel.IClassification>();
        var allProperties = new List<GuidelineModel.IProperty>();
        var allPropertySets = new List<GuidelineModel.IPropertySet>();

        foreach (var version in versions)
        {
            var propsById = new Dictionary<string, Entities.GuidelineProperty>();
            foreach (var p in version.Properties)
                propsById[p.PropertyId] = p;

            var setsById = new Dictionary<string, Entities.GuidelinePropertySet>();
            foreach (var ps in version.PropertySets)
                setsById[ps.PropertySetId] = ps;

            foreach (var p in version.Properties)
                allProperties.Add(ReconstructProperty(p));

            foreach (var ps in version.PropertySets)
                allPropertySets.Add(ReconstructPropertySet(ps));

            foreach (var c in version.Classifications)
                allClassifications.Add(ReconstructClassification(c, propsById, setsById));
        }

        var meta = versions.Count > 0 ? DeserializeDomainMeta(versions[0].DomainJson) : null;
        var first = versions.FirstOrDefault();

        return new GuidelineModel.Guideline
        {
            Identifier = first?.GuidelineId,
            Name = first?.Name,
            Description = first?.Description,
            Version = first?.Version,
            Status = ParseStatus(meta?.Status),
            ComplexData = first is null ? null : DeserializeJson<GuidelineModel.ComplexData>(first.ComplexDataJson),
            Domain = new GuidelineModel.Domain
            {
                ID = meta?.ID,
                Name = meta?.Name,
                Identifier = meta?.Identifier,
                Description = meta?.Description,
                Version = meta?.Version,
                Status = ParseStatus(meta?.Status),
                Classifications = allClassifications,
                Properties = allProperties,
                PropertySets = allPropertySets
            }
        };
    }

    private static GuidelineModel.Classification ReconstructClassification(
        Entities.GuidelineClassification gc,
        Dictionary<string, Entities.GuidelineProperty> properties,
        Dictionary<string, Entities.GuidelinePropertySet> propertySets)
    {
        var cps = gc.ClassificationProperties
            .Select(cp => ReconstructClassificationProperty(cp, properties, propertySets))
            .Cast<GuidelineModel.IClassificationProperty>()
            .ToList();

        return new GuidelineModel.Classification
        {
            Identifier = gc.ClassificationId,
            Name = gc.Name,
            Code = gc.Code,
            Description = gc.Description,
            Status = ParseStatus(gc.Status),
            ClassificationProperties = cps,
            Parent = null,
            Children = null
        };
    }

    private static GuidelineModel.ClassificationProperty ReconstructClassificationProperty(
        Entities.GuidelineClassificationProperty cp,
        Dictionary<string, Entities.GuidelineProperty> properties,
        Dictionary<string, Entities.GuidelinePropertySet> propertySets)
    {
        GuidelineModel.IPropertyAssignment? assignment = null;
        GuidelineModel.IProperty? prop = null;

        if (properties.TryGetValue(cp.PropertyId, out var gp))
            prop = ReconstructProperty(gp);

        if (cp.AssignmentJson != null)
            assignment = ReconstructAssignment(cp.AssignmentJson, prop);
        else if (prop != null)
            assignment = new GuidelineModel.PropertyAssignment { Property = prop };

        GuidelineModel.IPropertySet? propertySet = null;
        if (cp.PropertySetId != null && propertySets.TryGetValue(cp.PropertySetId, out var gps))
            propertySet = ReconstructPropertySet(gps);

        return new GuidelineModel.ClassificationProperty
        {
            Identifier = cp.ClassificationPropertyId,
            IsRequired = cp.IsRequired,
            SortNumber = cp.SortNumber,
            IsReadonly = cp.IsReadonly,
            DefaultValue = cp.DefaultValue,
            Reference = cp.Reference,
            PropertyAssignment = assignment,
            PropertySet = propertySet
        };
    }

    private static GuidelineModel.IProperty ReconstructProperty(Entities.GuidelineProperty gp)
    {
        var storageType = Enum.TryParse<StorageType>(gp.StorageType, out var st) ? st : StorageType.String;
        var status = ParseStatus(gp.Status);

        switch (gp.PropertyType)
        {
            case nameof(GuidelineModel.PropertyEnum):
                return new GuidelineModel.PropertyEnum
                {
                    Identifier = gp.PropertyId,
                    Name = gp.Name,
                    Description = gp.Description,
                    StorageType = storageType,
                    Code = gp.Code,
                    UnitType = gp.UnitType,
                    UnitAbbreviation = gp.UnitAbbreviation,
                    Status = status,
                    Enums = DeserializeJson<List<GuidelineModel.PropertyEnumItem>>(gp.ExtraJson)
                };
            case nameof(GuidelineModel.PropertySimple):
            {
                var extra = GuidelineJson.Deserialize<RangeExtra>(gp.ExtraJson);
                return new GuidelineModel.PropertySimple
                {
                    Identifier = gp.PropertyId,
                    Name = gp.Name,
                    Description = gp.Description,
                    StorageType = storageType,
                    Code = gp.Code,
                    UnitType = gp.UnitType,
                    UnitAbbreviation = gp.UnitAbbreviation,
                    Status = status,
                    Min = extra?.Min,
                    MinIsInclusive = extra?.MinIsInclusive ?? false,
                    Max = extra?.Max,
                    MaxIsInclusive = extra?.MaxIsInclusive ?? false
                };
            }
            case nameof(GuidelineModel.PropertySuperEnum):
            {
                var extra = GuidelineJson.Deserialize<SuperEnumExtra>(gp.ExtraJson);
                return new GuidelineModel.PropertySuperEnum
                {
                    Identifier = gp.PropertyId,
                    Name = gp.Name,
                    Description = gp.Description,
                    StorageType = storageType,
                    Code = gp.Code,
                    UnitType = gp.UnitType,
                    UnitAbbreviation = gp.UnitAbbreviation,
                    Status = status,
                    Level = extra?.Level ?? 0,
                    Item = extra?.Item
                };
            }
            case nameof(GuidelineModel.PropertyTree):
                return new GuidelineModel.PropertyTree
                {
                    Identifier = gp.PropertyId,
                    Name = gp.Name,
                    Description = gp.Description,
                    StorageType = storageType,
                    Code = gp.Code,
                    UnitType = gp.UnitType,
                    UnitAbbreviation = gp.UnitAbbreviation,
                    Status = status,
                    Item = DeserializeJson<GuidelineModel.ComplexDataItem>(gp.ExtraJson)
                };
            default:
                return new GuidelineModel.PropertySimple
                {
                    Identifier = gp.PropertyId,
                    Name = gp.Name,
                    Description = gp.Description,
                    StorageType = storageType,
                    Code = gp.Code,
                    UnitType = gp.UnitType,
                    UnitAbbreviation = gp.UnitAbbreviation,
                    Status = status
                };
        }
    }

    private static GuidelineModel.PropertySet ReconstructPropertySet(Entities.GuidelinePropertySet gps)
    {
        return new GuidelineModel.PropertySet
        {
            Identifier = gps.PropertySetId,
            Name = gps.Name,
            Description = gps.Description,
            Status = ParseStatus(gps.Status)
        };
    }

    private static GuidelineModel.IPropertyAssignment? ReconstructAssignment(string assignmentJson, GuidelineModel.IProperty? prop)
    {
        // AssignmentJson is a flat blob written by the transformation with an explicit "Type"
        // discriminator, so it is read as a document rather than deserialized into a model type.
        using var document = JsonDocument.Parse(assignmentJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        var type = root.TryGetProperty("Type", out var typeElement) ? typeElement.GetString() : null;

        return type switch
        {
            nameof(GuidelineModel.PropertyEnumAssignment) => new GuidelineModel.PropertyEnumAssignment
            {
                Property = prop,
                FreeTextEnabled = GetBoolean(root, "FreeTextEnabled"),
                SelectedEnum = root.TryGetProperty("SelectedEnum", out var se) && se.ValueKind == JsonValueKind.Object
                    ? se.Deserialize<GuidelineModel.PropertyEnumItem>(GuidelineJson.Options)
                    : null
            },
            nameof(GuidelineModel.PropertySimpleAssignment) => new GuidelineModel.PropertySimpleAssignment
            {
                Property = prop,
                Min = GetString(root, "Min"),
                MinIsInclusive = GetBoolean(root, "MinIsInclusive"),
                Max = GetString(root, "Max"),
                MaxIsInclusive = GetBoolean(root, "MaxIsInclusive")
            },
            nameof(GuidelineModel.PropertySuperEnumAssignment) => new GuidelineModel.PropertySuperEnumAssignment
            {
                Property = prop
            },
            _ => new GuidelineModel.PropertyAssignment { Property = prop }
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool GetBoolean(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;

    private static Status ParseStatus(string? statusStr)
        => Enum.TryParse<Status>(statusStr, out var status) ? status : Status.Active;

    private static T? DeserializeJson<T>(string? json) where T : class
        => GuidelineJson.Deserialize<T>(json);

    private static DomainMeta? DeserializeDomainMeta(string? json)
        => GuidelineJson.Deserialize<DomainMeta>(json);

    /// <summary>
    /// Internal DTO for the ExtraJson blob of a <see cref="GuidelineModel.PropertySimple"/>. Plain settable
    /// properties, no constructor — reference preservation does not support parameterized constructors.
    /// </summary>
    private sealed class RangeExtra
    {
        public string? Min { get; set; }
        public bool MinIsInclusive { get; set; }
        public string? Max { get; set; }
        public bool MaxIsInclusive { get; set; }
    }

    /// <summary>Internal DTO for the ExtraJson blob of a <see cref="GuidelineModel.PropertySuperEnum"/>.</summary>
    private sealed class SuperEnumExtra
    {
        public int Level { get; set; }
        public GuidelineModel.ComplexDataItem? Item { get; set; }
    }

    /// <summary>Internal DTO for deserializing the DomainJson metadata blob.</summary>
    private sealed class DomainMeta
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? Identifier { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Version { get; set; }
    }
}
