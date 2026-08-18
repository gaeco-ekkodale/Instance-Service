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

public class GuidelineNameResolver :
    IValueResolver<Instance, Dto.Instance, string>,
    IValueResolver<(Instance instance, Accessibility accessibility), Dto.Instance, string>
{
    private readonly IGuidelineReconstructionService _reconstruction;

    public GuidelineNameResolver(IGuidelineReconstructionService reconstruction)
    {
        _reconstruction = reconstruction;
    }

    public string Resolve(Instance source, Dto.Instance destination, string destMember, ResolutionContext context)
    {
        return GetGuidelineName(source.ClassificationId);
    }

    public string Resolve((Instance instance, Accessibility accessibility) source, Dto.Instance destination, string destMember, ResolutionContext context)
    {
        return GetGuidelineName(source.instance.ClassificationId);
    }

    private string GetGuidelineName(string classificationId)
    {
        var summaries = _reconstruction.GetClassificationSummariesAsync().GetAwaiter().GetResult();
        var summary = summaries.FirstOrDefault(s => s.Id == classificationId);
        return summary?.GuidelineName ?? string.Empty;
    }
}
