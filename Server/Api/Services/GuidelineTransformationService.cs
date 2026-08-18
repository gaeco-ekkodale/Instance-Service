// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Model;
using InstanceService.Api.Messaging.Consumers.Guidelines;
using InstanceService.Domain.IRepositories;
using InstanceService.Models.Guideline;
using GuidelineModelIO;
using InstanceService.Api.Serialization;
using Minio;
using Minio.DataModel.Args;

namespace InstanceService.Api.Services;

/// <summary>
/// Processes guideline events from the GuidelineService. On upload it loads the guideline from object
/// storage, transforms it into the relational projection (GuidelineVersion → Classifications → Properties),
/// and persists it idempotently. When the change removes classifications (or the guideline is deleted),
/// it deletes the graph instances of the classes that no longer exist in the guideline.
/// Uses streaming deserialization and temp files to handle large guideline files without excessive memory usage.
/// </summary>
public interface IGuidelineTransformationService
{
    /// <summary>Processes an upload/replace event: download, transform, upsert, and clean up orphaned instances.</summary>
    Task ProcessAsync(UploadedGuideline uploadedGuideline, CancellationToken cancellationToken = default);

    /// <summary>Processes a delete event: remove the projection and delete all instances of its classifications.</summary>
    Task DeleteAsync(DeletedGuideline deletedGuideline, CancellationToken cancellationToken = default);
}

/// <inheritdoc/>
public class GuidelineTransformationService : IGuidelineTransformationService
{
    private readonly IMinioClient _minioClient;
    private readonly IGuidelineProjectionRepository _repository;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IGuidelineReconstructionService _reconstruction;
    private readonly ILogger<GuidelineTransformationService> _logger;

    /// <summary>
    /// Reading the guideline file is delegated to the Guideline.Model package, which owns the on-disk
    /// schema. Hand-rolled serializer settings cannot keep up with it: since SchemaVersion 2.0 the file
    /// no longer carries a type discriminator for classifications, which is why the previous
    /// Newtonsoft-based reader failed with "Could not create an instance of type IClassification".
    /// The reader handles both the 2.0 format and older files that still carry <c>$type</c> everywhere.
    /// </summary>
    private static readonly GuidelineReaderWriter GuidelineReader = new();

    public GuidelineTransformationService(
        IMinioClient minioClient,
        IGuidelineProjectionRepository repository,
        IInstanceRepository instanceRepository,
        IGuidelineReconstructionService reconstruction,
        ILogger<GuidelineTransformationService> logger)
    {
        _minioClient = minioClient;
        _repository = repository;
        _instanceRepository = instanceRepository;
        _reconstruction = reconstruction;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(UploadedGuideline uploadedGuideline, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing guideline upload event: Name={Name}, ObjectKey={ObjectKey}, Etag={Etag}, CorrelationId={CorrelationId}",
            uploadedGuideline.Name, uploadedGuideline.ObjectKey, uploadedGuideline.Etag, uploadedGuideline.CorrelationId);

        var guidelineId = ParseGuidelineId(uploadedGuideline.Id);

        // Early-exit if this exact version is already persisted (same guideline id + same Etag).
        // The full idempotency guard is also inside UpsertAsync (advisory lock), but this avoids
        // an unnecessary download.
        if (await _repository.ExistsAsync(guidelineId, uploadedGuideline.Etag, cancellationToken))
        {
            _logger.LogInformation(
                "Guideline {Id} with Etag={Etag} already processed. Skipping.",
                guidelineId, uploadedGuideline.Etag);
            return;
        }

        var tempFilePath = Path.GetTempFileName();
        try
        {
            // Step 1: Stream from object storage to temp file
            await DownloadToTempFileAsync(uploadedGuideline.BucketName, uploadedGuideline.ObjectKey, tempFilePath, cancellationToken);

            // Step 2: Streaming-deserialize from temp file
            Guideline.Model.Model.Guideline guideline;
            try
            {
                guideline = DeserializeGuideline(tempFilePath, uploadedGuideline.ObjectKey);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex,
                    "I/O error reading temp file for Name={Name}. File may be corrupted or inaccessible.",
                    uploadedGuideline.Name);
                throw;
            }

            // Step 3: Transform into relational model
            var version = TransformToRelationalModel(guideline, uploadedGuideline);

            // Step 4: Persist idempotently (replaces previous version for same GuidelineService ID)
            var upsertResult = await _repository.UpsertAsync(version, cancellationToken);

            // The projection changed — drop the cached reconstructed guideline so reads rebuild it.
            _reconstruction.Invalidate();

            _logger.LogInformation(
                "Successfully persisted guideline projection: Name={Name}, ObjectKey={ObjectKey}, Etag={Etag}, " +
                "Classifications={ClassCount}, Properties={PropCount}, ClassificationProperties={CpCount}",
                uploadedGuideline.Name, uploadedGuideline.ObjectKey, uploadedGuideline.Etag,
                version.Classifications.Count, version.Properties.Count,
                version.Classifications.Sum(c => c.ClassificationProperties.Count));

            // Step 5: Delete instances of classifications that no longer exist in the changed guideline
            if (upsertResult.RemovedClassificationIds.Count > 0)
            {
                try
                {
                    var deleted = await _instanceRepository.DeleteInstancesByClassificationIds(
                        upsertResult.RemovedClassificationIds);
                    _logger.LogInformation(
                        "Deleted {InstanceCount} instances belonging to {ClassCount} removed classifications after guideline change. ServiceId={ServiceId}",
                        deleted, upsertResult.RemovedClassificationIds.Count, uploadedGuideline.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to delete instances of removed classifications after guideline upsert. ServiceId={ServiceId}",
                        uploadedGuideline.Id);
                }
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(DeletedGuideline deletedGuideline, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing guideline delete event: Id={Id}, ObjectKey={ObjectKey}",
            deletedGuideline.Id, deletedGuideline.ObjectKey);

        var guidelineId = ParseGuidelineId(deletedGuideline.Id);

        // Collect all classification IDs before the cascade delete removes them, so we know which
        // classes' instances must be deleted from the graph.
        var classificationIds = await _repository.GetClassificationIdsByIdAsync(
            guidelineId, cancellationToken);

        if (classificationIds.Count > 0)
        {
            try
            {
                var deleted = await _instanceRepository.DeleteInstancesByClassificationIds(classificationIds);
                _logger.LogInformation(
                    "Deleted {InstanceCount} instances belonging to {ClassCount} classifications of deleted guideline Id={Id}.",
                    deleted, classificationIds.Count, deletedGuideline.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to delete instances before guideline deletion. Id={Id}", deletedGuideline.Id);
            }
        }

        await _repository.DeleteByIdAsync(guidelineId, cancellationToken);

        // The projection changed — drop the cached reconstructed guideline so reads rebuild it.
        _reconstruction.Invalidate();

        _logger.LogInformation("Deleted guideline projection for ObjectKey={ObjectKey}.", deletedGuideline.ObjectKey);
    }

    /// <summary>
    /// Parses the guideline id carried by the event (the GuidelineService's GUID) into the projection
    /// primary key. Keeps the identifier identical across services.
    /// </summary>
    private static Guid ParseGuidelineId(string eventId)
    {
        if (Guid.TryParse(eventId, out var id))
            return id;
        throw new InvalidOperationException(
            $"Guideline event Id '{eventId}' is not a valid GUID; cannot use it as the projection primary key.");
    }

    private async Task DownloadToTempFileAsync(string bucketName, string objectName, string tempFilePath, CancellationToken cancellationToken)
    {
        bool bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName), cancellationToken);

        if (!bucketExists)
        {
            throw new FileNotFoundException($"Bucket '{bucketName}' does not exist.");
        }

        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithFile(tempFilePath), cancellationToken);

        _logger.LogDebug("Downloaded guideline {ObjectName} from {BucketName} ({Size} bytes)",
            objectName, bucketName, new FileInfo(tempFilePath).Length);
    }

    /// <summary>
    /// Stream-deserializes the guideline from a temp file using JsonTextReader
    /// so the raw JSON string is never held in memory alongside the object graph.
    /// </summary>
    /// <summary>
    /// Reads the guideline from the downloaded temp file via the Guideline.Model reader,
    /// which resolves the concrete model types for the schema the file was written in.
    /// </summary>
    private static Guideline.Model.Model.Guideline DeserializeGuideline(string tempFilePath, string objectKey)
    {
        var guideline = GuidelineReader.GuidelineRead(tempFilePath);

        return guideline as Guideline.Model.Model.Guideline
               ?? throw new InvalidOperationException(
                   $"Guideline '{objectKey}' deserialized to '{guideline?.GetType().Name ?? "null"}' " +
                   $"instead of {nameof(Guideline.Model.Model.Guideline)}.");
    }

    /// <summary>
    /// Transforms the deserialized Guideline.Model into the relational domain model.
    /// Business-relevant fields become proper columns; everything else is serialized as compact JSON blobs.
    /// </summary>
    private GuidelineVersion TransformToRelationalModel(Guideline.Model.Model.Guideline guideline, UploadedGuideline evt)
    {
        var version = new GuidelineVersion
        {
            Id = ParseGuidelineId(evt.Id),
            GuidelineId = guideline.Identifier ?? throw new InvalidOperationException(
                $"Guideline Identifier is null or empty for ObjectKey='{evt.ObjectKey}'."),
            Name = evt.Name,
            Identifier = guideline.Identifier,
            Description = guideline.Description,
            Version = guideline.Version,
            ObjectName = evt.ObjectKey,
            BucketName = evt.BucketName,
            Etag = evt.Etag,
            CorrelationId = evt.CorrelationId,
            EventTimestamp = evt.Timestamp,
            ProcessedAt = DateTimeOffset.UtcNow,
            MappingsJson = GuidelineJson.SerializeCompact(guideline.Mappings),
            ComplexDataJson = GuidelineJson.SerializeCompact(guideline.ComplexData),
            DomainJson = SerializeDomainMeta(guideline.Domain)
        };

        // Transform domain-level property definitions (deduplicate by PropertyId, keep last occurrence)
        if (guideline.Domain?.Properties != null)
        {
            var deduplicatedProperties = DeduplicateByKey(
                guideline.Domain.Properties, p => p.Identifier!, "Property", evt.Name);
            foreach (var prop in deduplicatedProperties)
            {
                version.Properties.Add(TransformProperty(prop, version.Id));
            }
        }

        // Transform domain-level property sets (deduplicate by PropertySetId, keep last occurrence)
        if (guideline.Domain?.PropertySets != null)
        {
            var deduplicatedPropertySets = DeduplicateByKey(
                guideline.Domain.PropertySets, ps => ps.Identifier!, "PropertySet", evt.Name);
            foreach (var ps in deduplicatedPropertySets)
            {
                version.PropertySets.Add(new GuidelinePropertySet
                {
                    Id = Guid.NewGuid(),
                    GuidelineVersionId = version.Id,
                    PropertySetId = ps.Identifier!,
                    Name = ps.Name ?? string.Empty,
                    Identifier = ps.Identifier,
                    Description = ps.Description,
                    Status = ps.Status.ToString()
                });
            }
        }

        // Transform classifications with their classification properties (deduplicate by ClassificationId, keep last occurrence)
        if (guideline.Domain?.Classifications != null)
        {
            var deduplicatedClassifications = DeduplicateByKey(
                 guideline.Domain.Classifications, cls => cls.Identifier!, "Classification", evt.Name);
            foreach (var cls in deduplicatedClassifications)
            {
                version.Classifications.Add(TransformClassification(cls, version.Id, evt.Name));
            }
        }

        return version;
    }

    /// <summary>
    /// Validates that all items have unique, non-null/empty keys.
    /// Throws if any key is null/empty or if duplicate keys are found.
    /// </summary>
    private IReadOnlyList<T> DeduplicateByKey<T>(
        IEnumerable<T> items, Func<T, string> keySelector, string entityType, string objectName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<T>();

        foreach (var item in items)
        {
            var key = keySelector(item);
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException(
                    $"{entityType} has a null or empty key in guideline '{objectName}'.");
            }

            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    $"Duplicate {entityType} with key '{key}' found in guideline '{objectName}'.");
            }

            list.Add(item);
        }

        return list;
    }

    private GuidelineClassification TransformClassification(IClassification cls, Guid versionId, string objectName)
    {
        var gc = new GuidelineClassification
        {
            Id = Guid.NewGuid(),
            GuidelineVersionId = versionId,
            ClassificationId = cls.Identifier ?? throw new InvalidOperationException(
                $"Classification has a null or empty Identifier in guideline '{objectName}'."),
            Name = cls.Name ?? string.Empty,
            Identifier = cls.Identifier,
            Code = cls.Code,
            Description = cls.Description,
            Status = cls.Status.ToString(),
            RelationsJson = SerializeRelations(cls)
        };

        if (cls.ClassificationProperties != null)
        {
            // Silently deduplicate by Identifier (the DB unique key), keeping the first occurrence.
            // Structural validation is the GuidelineService's responsibility; we only adapt the data
            // to fit the relational DB constraints.
            var seenIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cp in cls.ClassificationProperties)
            {
                if (!string.IsNullOrEmpty(cp.Identifier) && seenIdentifiers.Add(cp.Identifier))
                {
                    gc.ClassificationProperties.Add(TransformClassificationProperty(cp, gc.Id));
                }
                else
                {
                    _logger.LogWarning(
                        "Skipping ClassificationProperty with duplicate or empty Identifier '{Identifier}' in Classification '{ClassificationId}' of guideline '{ObjectName}'.",
                        cp.Identifier, cls.Identifier, objectName);
                }
            }
        }

        return gc;
    }

    private static GuidelineClassificationProperty TransformClassificationProperty(IClassificationProperty cp, Guid classificationId)
    {
        var propertyId = cp.PropertyAssignment?.Property?.Identifier ?? string.Empty;

        return new GuidelineClassificationProperty
        {
            Id = Guid.NewGuid(),
            GuidelineClassificationId = classificationId,
            ClassificationPropertyId = cp.Identifier,
            PropertyId = propertyId,
            PropertySetId = cp.PropertySet?.Identifier,
            IsRequired = cp.IsRequired,
            SortNumber = cp.SortNumber,
            IsReadonly = cp.IsReadonly,
            DefaultValue = cp.DefaultValue,
            Reference = cp.Reference,
            AssignmentJson = SerializeAssignment(cp.PropertyAssignment)
        };
    }

    private static GuidelineProperty TransformProperty(IProperty prop, Guid versionId)
    {
        string? extraJson = null;
        string? propertyType;

        switch (prop)
        {
            case PropertySuperEnum pse:
                propertyType = nameof(PropertySuperEnum);
                extraJson = GuidelineJson.SerializeCompact(new
                {
                    pse.Level,
                    Item = pse.Item
                });
                break;
            case PropertyEnum pe:
                propertyType = nameof(PropertyEnum);
                extraJson = GuidelineJson.SerializeCompact(pe.Enums);
                break;
            case PropertySimple ps:
                propertyType = nameof(PropertySimple);
                if (ps.Min != null || ps.Max != null)
                {
                    extraJson = GuidelineJson.SerializeCompact(new
                    {
                        ps.Min,
                        ps.MinIsInclusive,
                        ps.Max,
                        ps.MaxIsInclusive
                    });
                }
                break;
            case PropertyTree pt:
                propertyType = nameof(PropertyTree);
                extraJson = GuidelineJson.SerializeCompact(pt.Item);
                break;
            default:
                propertyType = prop.GetType().Name;
                break;
        }

        return new GuidelineProperty
        {
            Id = Guid.NewGuid(),
            GuidelineVersionId = versionId,
            PropertyId = prop.Identifier ?? throw new InvalidOperationException(
                "Property has a null or empty Identifier."),
            Name = prop.Name ?? string.Empty,
            Identifier = prop.Identifier,
            Description = prop.Description,
            StorageType = prop.StorageType.ToString(),
            Code = prop.Code,
            UnitType = prop.UnitType,
            UnitAbbreviation = prop.UnitAbbreviation,
            Status = prop.Status.ToString(),
            PropertyType = propertyType,
            ExtraJson = extraJson
        };
    }

    /// <summary>
    /// Serializes parent/children classification relations to a compact JSON string.
    /// Only stores IDs to avoid circular references and keep the blob small.
    /// </summary>
    private static string? SerializeRelations(IClassification cls)
    {
        var parentId = cls.Parent?.Item?.Identifier;
        var childIds = cls.Children?.Select(c => c.Item?.Identifier).Where(id => id != null).ToList();

        if (parentId == null && (childIds == null || childIds.Count == 0))
            return null;

        return GuidelineJson.SerializeCompact(new
        {
            ParentId = parentId,
            ChildIds = childIds
        });
    }

    /// <summary>
    /// Serializes domain-level metadata (ID, Name, Identifier, etc.) to JSON.
    /// The domain's collections (Classifications, Properties, PropertySets) are stored relationally, not here.
    /// </summary>
    private static string? SerializeDomainMeta(IDomain? domain)
    {
        if (domain == null)
            return null;
        return GuidelineJson.SerializeCompact(new
        {
            domain.ID,
            domain.Name,
            domain.Identifier,
            domain.Description,
            Status = domain.Status.ToString(),
            domain.Version
        });
    }

    /// <summary>
    /// Serializes the PropertyAssignment details to JSON, excluding the Property reference
    /// (which is already stored relationally via PropertyId).
    /// </summary>
    private static string? SerializeAssignment(IPropertyAssignment? assignment)
    {
        if (assignment == null)
            return null;

        return assignment switch
        {
            PropertyEnumAssignment pea => GuidelineJson.SerializeCompact(new
            {
                Type = nameof(PropertyEnumAssignment),
                pea.FreeTextEnabled,
                SelectedEnum = pea.SelectedEnum != null ? new
                {
                    pea.SelectedEnum.ID,
                    pea.SelectedEnum.Name
                } : null
            }),
            PropertySimpleAssignment psa => GuidelineJson.SerializeCompact(new
            {
                Type = nameof(PropertySimpleAssignment),
                psa.Min,
                psa.MinIsInclusive,
                psa.Max,
                psa.MaxIsInclusive
            }),
            PropertySuperEnumAssignment psea => GuidelineJson.SerializeCompact(new
            {
                Type = nameof(PropertySuperEnumAssignment),
                ParentId = psea.Parent?.ID
            }),
            _ => GuidelineJson.SerializeCompact(new { Type = assignment.GetType().Name, assignment.ID })
        };
    }

}
