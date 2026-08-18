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
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using Messaging.Core.Abstractions;
using InstanceService.Models;

namespace InstanceService.Api.Messaging.Consumers.Classifications;

/// <summary>
/// Consumes a request to retrieve a list of classifications filtered by user access rights.
/// </summary>
public class GetClassificationsFilteredConsumer : IInternalRequestConsumer<GetClassificationsFiltered, ClassificationListResponse>
{
    /// <summary>
    /// Gets the logger for this consumer.
    /// </summary>
    public ILogger<IInternalRequestConsumer<GetClassificationsFiltered, ClassificationListResponse>> Logger { get; }
    private readonly IGuidelineReconstructionService _reconstruction;
    private readonly IUserGroupProvider _userGroupProvider;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IAccessRightHelper _accessRightHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetClassificationsFilteredConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reconstruction">Provides the cached lightweight classification-name lookup.</param>
    /// <param name="userGroupProvider">The provider for user group information.</param>
    /// <param name="accessRightsFetcher">The fetcher for access rights.</param>
    /// <param name="accessRightHelper">The helper for checking access rights.</param>
    public GetClassificationsFilteredConsumer(
        ILogger<IInternalRequestConsumer<GetClassificationsFiltered, ClassificationListResponse>> logger,
        IGuidelineReconstructionService reconstruction,
        IUserGroupProvider userGroupProvider,
        IAccessRightsFetcher accessRightsFetcher,
        IAccessRightHelper accessRightHelper)
    {
        Logger = logger;
        _reconstruction = reconstruction;
        _userGroupProvider = userGroupProvider;
        _accessRightsFetcher = accessRightsFetcher;
        _accessRightHelper = accessRightHelper;
    }

    /// <summary>
    /// Consumes the request to get filtered classifications.
    /// </summary>
    /// <param name="request">The request containing the token and use case ID.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation, containing the response with the filtered classifications.</returns>
    /// <exception cref="ArgumentException">Thrown if the request token is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an unexpected error occurs during processing.</exception>
    public async Task<ClassificationListResponse> ConsumeInternal(GetClassificationsFiltered request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                throw new ArgumentException("Token is required", nameof(request));
            }

            var groupIds = await GetUserGroupIdsAsync(request.Token);
            var useCaseId = request.Id;
            var accessRights = await GetAccessRightsAsync();

            // Use the cached lightweight summaries (id + name + guideline name) instead of reconstructing
            // the full guideline. GuidelineName lets the UI disambiguate same-named classifications.
            var summaries = await _reconstruction.GetClassificationSummariesAsync();

            var filteredClassifications = FilterClassificationsByAccess(summaries, groupIds, accessRights, useCaseId);

            Logger.LogInformation("Successfully processed {FilteredCount} classifications from {TotalCount} total",
                filteredClassifications.Count, summaries.Count);

            return new ClassificationListResponse
            {
                Classifications = new ClassificationsListSet
                {
                    Classifications = filteredClassifications
                }
            };
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Invalid request parameters for GetClassificationsFiltered");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Operation failed while processing GetClassificationsFiltered request");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error processing GetClassificationsFiltered request");
            throw new InvalidOperationException("An unexpected error occurred while processing the request", ex);
        }
    }

    /// <summary>
    /// Asynchronously retrieves user group identifiers using the provided authentication token.
    /// </summary>
    /// <param name="token">The authentication token of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of user group IDs.</returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve user group information.</exception>
    private async Task<List<string>> GetUserGroupIdsAsync(string token)
    {
        try
        {
            var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(token);
            if (groupIds == null)
            {
                return new List<string>();
            }
            return groupIds;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch user group IDs for token");
            throw new InvalidOperationException("Unable to retrieve user group information", ex);
        }
    }

    /// <summary>
    /// Asynchronously retrieves the list of all access rights.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of access rights.</returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve access rights.</exception>
    private async Task<List<AccessRight>> GetAccessRightsAsync()
    {
        try
        {
            var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
            if (accessRights == null)
            {
                return new List<AccessRight>();
            }
            return accessRights.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch access rights from external service");
            throw new InvalidOperationException("Unable to retrieve access rights", ex);
        }
    }

    /// <summary>
    /// Filters the classification summaries down to those the user has write access to.
    /// </summary>
    private List<InstanceService.Models.ClassificationList> FilterClassificationsByAccess(
        IReadOnlyList<ClassificationSummary> summaries,
        List<string> groupIds,
        List<AccessRight> accessRights,
        string useCaseId)
    {
        try
        {
            return summaries
                .Where(s => _accessRightHelper.HasWriteAccessibility(s.Id, groupIds, accessRights, useCaseId))
                .Select(s => new InstanceService.Models.ClassificationList
                {
                    Id = s.Id,
                    Name = s.Name,
                    GuidelineName = s.GuidelineName
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to filter classifications");
            return new List<InstanceService.Models.ClassificationList>();
        }
    }
}