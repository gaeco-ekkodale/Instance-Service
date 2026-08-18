// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AutoMapper;
using InstanceService.Api.Services;
using InstanceService.Models;
using InstanceService.Models.Enum;

namespace InstanceService.Api.MapperProfiles;

/// <summary>
/// Resolves the name of a classification based on its ID for AutoMapper mappings.
/// </summary>
public class ClassificationNameResolver :
    IValueResolver<Instance, Dto.Instance, string>,
    IValueResolver<(Instance instance, Accessibility accessibility), Dto.Instance, string>
{
    private readonly IGuidelineReconstructionService _reconstruction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationNameResolver"/> class.
    /// </summary>
    /// <param name="reconstruction">Provides the cached lightweight classification-name lookup.</param>
    public ClassificationNameResolver(IGuidelineReconstructionService reconstruction)
    {
        _reconstruction = reconstruction;
    }

    /// <summary>
    /// Resolves the classification name for a given source instance.
    /// </summary>
    /// <param name="source">The source instance object.</param>
    /// <param name="destination">The destination instance DTO object.</param>
    /// <param name="destMember">The destination member to map.</param>
    /// <param name="context">The resolution context.</param>
    /// <returns>The name of the classification.</returns>
    public string Resolve(Instance source, Dto.Instance destination, string destMember, ResolutionContext context)
    {
        return GetClassificationName(source.ClassificationId);
    }

    /// <summary>
    /// Resolves the classification name from a source tuple containing an instance.
    /// </summary>
    /// <param name="source">The source tuple containing the instance and accessibility information.</param>
    /// <param name="destination">The destination instance DTO object.</param>
    /// <param name="destMember">The destination member to map.</param>
    /// <param name="context">The resolution context.</param>
    /// <returns>The name of the classification.</returns>
    public string Resolve((Instance instance, Accessibility accessibility) source, Dto.Instance destination, string destMember, ResolutionContext context)
    {
        return GetClassificationName(source.instance.ClassificationId);
    }

    /// <summary>
    /// Retrieves the classification name for a given classification ID from the cached name lookup.
    /// Falls back to the ID itself if the classification is not (yet) present in any stored guideline,
    /// so rendering a graph never fails just because a name is missing.
    /// </summary>
    /// <param name="classificationId">The identifier of the classification to find.</param>
    /// <returns>The classification name, or the ID if unknown.</returns>
    private string GetClassificationName(string classificationId)
    {
        var names = _reconstruction.GetClassificationNamesAsync().GetAwaiter().GetResult();
        return names.TryGetValue(classificationId, out var name) && !string.IsNullOrEmpty(name)
            ? name
            : classificationId;
    }
}
