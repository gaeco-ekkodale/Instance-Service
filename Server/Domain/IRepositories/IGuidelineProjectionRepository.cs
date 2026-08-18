// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models.Guideline;

namespace InstanceService.Domain.IRepositories;

/// <summary>
/// Persists the relational guideline projection the InstanceService builds from uploaded guideline files.
/// The guideline's GUID (taken from the event, used as the primary key) is the stable identity key for
/// upserts: the first upload inserts the full projection; subsequent uploads do a granular in-place upsert
/// (classifications/properties/property-sets updated, added, or removed).
/// </summary>
public interface IGuidelineProjectionRepository
{
    /// <summary>
    /// Returns <c>true</c> if a guideline version with the given id and ETag is already stored.
    /// Lets the caller skip a redundant download/transform of an already-processed version.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, string etag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotently upserts the guideline projection (keyed by its id), replacing the previous version's
    /// child collections in place. Returns the classification and classification-property IDs that were
    /// present before but are absent from the new guideline, so the caller can clean up dependent data
    /// (e.g. graph instances of classes that no longer exist).
    /// </summary>
    Task<GuidelineUpsertResult> UpsertAsync(GuidelineVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all classification IDs belonging to the guideline version with the given id.
    /// Used before a cascade delete to learn which classes' instances must be removed.
    /// </summary>
    Task<List<string>> GetClassificationIdsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cascade-deletes the guideline version with the given id and all its child rows.
    /// A no-op if nothing matches.
    /// </summary>
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads every stored guideline (one row per guideline, keyed by ServiceId) together with all child
    /// collections (classifications + their properties, property sets, properties). Read-only.
    /// Used to reconstruct the full <c>Guideline.Model</c> object graph from the relational projection.
    /// </summary>
    Task<List<GuidelineVersion>> GetAllVersionsWithChildrenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a lightweight classification-id → name map across all stored guidelines. Cheap (no joins,
    /// no child collections) — used for hot paths that only need display names, not the full guideline.
    /// On duplicate classification IDs across guidelines the last one wins.
    /// </summary>
    Task<Dictionary<string, string>> GetClassificationNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a lightweight list of classification summaries (id, name, guideline name) across all stored
    /// guidelines. Includes the parent guideline name so the UI can disambiguate same-named classifications.
    /// </summary>
    Task<List<ClassificationSummary>> GetClassificationSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single classification (by its classification id) with its classification properties, plus only
    /// the <see cref="GuidelineProperty"/> and <see cref="GuidelinePropertySet"/> rows those properties reference.
    /// Avoids materializing the whole guideline when only one classification's properties are needed.
    /// Returns <c>null</c> if no classification with that id exists.
    /// </summary>
    Task<ClassificationGraph?> GetClassificationWithReferencesAsync(string classificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A single classification together with the property and property-set rows it references.
/// </summary>
public record ClassificationGraph(
    GuidelineClassification Classification,
    List<GuidelineProperty> Properties,
    List<GuidelinePropertySet> PropertySets);

/// <summary>Lightweight summary of a classification — id, display name, and parent guideline name.</summary>
public record ClassificationSummary(string Id, string Name, string GuidelineName);
