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
using InstanceService.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace InstanceService.Api.Utilities.Provider;

/// <summary>
/// Defines a contract for a guideline provider that retrieves guidelines.
/// </summary>
public interface IGuidelineProvider
{
    /// <summary>
    /// Asynchronously retrieves the guideline scoped to the specified use-case.
    /// </summary>
    /// <param name="useCaseId">The use-case identifier.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the guideline.</returns>
    Task<IGuideline> GetGuideline(string useCaseId);
}

/// <summary>
/// Provides the use-case-scoped guideline from the relational guideline projection.
/// The full guideline is reconstructed from the projection tables (all stored guidelines merged) and then
/// reduced to the classifications/properties referenced by the use-case's access rights — i.e. it returns
/// exactly what the (now removed) per-use-case guideline file used to contain, but sourced live from the
/// projection instead of a downloaded file.
/// </summary>
/// <param name="reconstruction">Reconstructs the full guideline graph from the projection.</param>
/// <param name="accessRightsFetcher">Fetches the access rights used to scope the guideline per use case.</param>
/// <param name="cache">Memoizes the reduced guideline per use case.</param>
/// <param name="logger">Logger forwarded to the guideline-reduction helper.</param>
public class GuidelineProvider(
    IGuidelineReconstructionService reconstruction,
    IAccessRightsFetcher accessRightsFetcher,
    IMemoryCache cache,
    ILogger<GuidelineProvider> logger) : IGuidelineProvider
{
    // The reduced guideline is requested once per instance during mapping (see ClassificationNameResolver and
    // the metadata consumers). Memoize it per use case so a single request doesn't re-fetch all access rights
    // and re-run the reduction N times. The key includes the reconstruction generation so a guideline change
    // takes effect immediately; the short TTL bounds staleness from access-right changes (which carry no event).
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public async Task<IGuideline> GetGuideline(string useCaseId)
    {
        var cacheKey = $"REDUCED_GUIDELINE::{reconstruction.Generation}::{useCaseId}";
        if (cache.TryGetValue(cacheKey, out IGuideline? cached) && cached is not null)
            return cached;

        var fullGuideline = await reconstruction.GetFullGuidelineAsync();

        // Scope to the use case: the access rights' classification/property IDs select which parts of the
        // guideline(s) this use case sees. (Access rights carry the GuidelineService classification/property IDs.)
        var accessRights = (await accessRightsFetcher.GetAccessRightsAsync())
            .Where(ar => ar.UseCaseId.ToString() == useCaseId)
            .ToList();

        var reduced = GuidelineHelper.GetReducedGuideline(logger, fullGuideline, accessRights);

        cache.Set(cacheKey, reduced, new MemoryCacheEntryOptions
        {
            Size = 1024,
            AbsoluteExpirationRelativeToNow = CacheTtl
        });

        return reduced;
    }
}
