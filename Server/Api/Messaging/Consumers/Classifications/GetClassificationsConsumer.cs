// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Messaging.Consumers.Classifications.Contracts;
using InstanceService.Api.Services;
using InstanceService.Api.Utilities.Provider;
using Messaging.Core.Abstractions;
using InstanceService.Models;

namespace InstanceService.Api.Messaging.Consumers.Classifications;

public class GetClassificationsConsumer(
    ILogger<IInternalRequestConsumer<GetClassifications, ClassificationsListSet>> logger,
    IGuidelineReconstructionService reconstruction,
    IAccessRightsFetcher accessRightsFetcher) : IInternalRequestConsumer<GetClassifications, ClassificationsListSet>
{
    public ILogger<IInternalRequestConsumer<GetClassifications, ClassificationsListSet>> Logger { get; } = logger;

    private readonly IGuidelineReconstructionService _reconstruction = reconstruction;
    private readonly IAccessRightsFetcher _accessRightsFetcher = accessRightsFetcher;

    /// <summary>
    /// Consumes the GetClassifications request. Returns the classifications of the use case (id + name),
    /// scoped by the use case's access rights and resolved against the lightweight name lookup — no full
    /// guideline reconstruction.
    /// </summary>
    /// <param name="request">The GetClassifications request message (Id = use case id).</param>
    public async Task<ClassificationsListSet> ConsumeInternal(GetClassifications request)
    {
        try
        {
            var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
            var names = await _reconstruction.GetClassificationNamesAsync();

            var classificationList = accessRights
                .Where(ar => ar.UseCaseId.ToString() == request.Id && !string.IsNullOrEmpty(ar.GuidelineClassificationId))
                .Select(ar => ar.GuidelineClassificationId!)
                .Distinct()
                .Select(id => new InstanceService.Models.ClassificationList
                {
                    Id = id,
                    Name = names.TryGetValue(id, out var name) ? name : id
                })
                .ToList();

            return new ClassificationsListSet
            {
                Classifications = classificationList
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while fetching classifications.");
            throw;
        }
    }
}
