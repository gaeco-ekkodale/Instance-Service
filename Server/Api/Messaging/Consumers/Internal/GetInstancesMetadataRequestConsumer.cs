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
using InstanceService.Api.Dto;
using GuidelineModel = Guideline.Model.Model;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Services;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Data;
using InstanceService.Models.Enum;
using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Internal;

/// <summary>
/// Represents a consumer for handling internal requests to get the metadata of multiple nodes.
/// </summary>
public class GetInstancesMetadataRequestConsumer : IInternalRequestConsumer<GetInstancesMetadataRequest, GetInstancesMetadataResponse>
{
    public ILogger<IInternalRequestConsumer<GetInstancesMetadataRequest, GetInstancesMetadataResponse>> Logger { get; }
    private readonly IGuidelineReconstructionService _reconstruction;
    private readonly InstanceServiceDbContext _context;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IAccessRightHelper _accessRightHelper;
    private readonly IUserGroupProvider _userGroupProvider;


    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstancesMetadataRequestConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="context">The database context.</param>
    /// <param name="accessRightsFetcher">The fetcher of access rights.</param>
    /// <param name="accessRightHelper">The helper class for access rights.</param>
    /// <param name="userGroupProvider">The provider of kc user groups.</param>
    /// <param name="reconstruction">Provides targeted single-classification reconstruction.</param>
    public GetInstancesMetadataRequestConsumer(
        ILogger<IInternalRequestConsumer<GetInstancesMetadataRequest, GetInstancesMetadataResponse>> logger,
        InstanceServiceDbContext context,
        IAccessRightsFetcher accessRightsFetcher,
        IAccessRightHelper accessRightHelper,
        IUserGroupProvider userGroupProvider,
        IGuidelineReconstructionService reconstruction
        )
    {
        _context = context;
        Logger = logger;
        _accessRightsFetcher = accessRightsFetcher;
        _accessRightHelper = accessRightHelper;
        _userGroupProvider = userGroupProvider;
        _reconstruction = reconstruction;
    }

    /// <summary>
    /// Consumes the internal request to get the metadata of multiple nodes.
    /// If there are multiple access rights for one property name, masstransit will interrupt consumption
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The response containing the metadata.</returns>
    public async Task<GetInstancesMetadataResponse> ConsumeInternal(GetInstancesMetadataRequest request)
    {
        IEnumerable<string> instanceIds = request.InstanceIds;
        string useCaseId = request.UseCaseId;

        List<InstanceData> instanceData = [];

        var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
        var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);

        foreach (var instanceId in instanceIds)
        {
            var singleMetadata = _context.InstanceMetadata
                .Where(x => x.Id == instanceId)
                .FirstOrDefault();
            if (singleMetadata == null)
                continue;
            
            var classificationId = singleMetadata.ClassificationId;
            var canGetMetadata = _accessRightHelper.CanGetMetadata(classificationId, groupIds, accessRights, useCaseId);

            if (!canGetMetadata)
                continue;

            var matchingClassification = await _reconstruction.GetClassificationAsync(classificationId);

            var filteredAccessRights = _accessRightHelper.GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);

            List<MetadataProperty> MetadataProperties = [];

            foreach (var classificationProperty in matchingClassification?.ClassificationProperties ?? [])
            {
                var property = classificationProperty?.PropertyAssignment?.Property;
                if (property == null) continue;

                var accessRight = _accessRightHelper.FilterSingleAccessRight(classificationProperty, filteredAccessRights, classificationId, useCaseId, groupIds);

                if (accessRight != null)
                {
                    singleMetadata.Properties.TryGetValue(property.Name, out var storedValue);

                    var storageType = property.StorageType;
                    var propertyType = property.GetType().Name;

                    IEnumerable<MetadataPropertyEnumValue> enumValues = [];
                    if (property is GuidelineModel.PropertyEnum pe && pe.Enums != null)
                    {
                        enumValues = pe.Enums.SelectMany(e => e.Values.Select(v => new MetadataPropertyEnumValue { Id = v.Key, Name = v.Value }));
                    }

                    string? min = null, max = null;
                    if (classificationProperty.PropertyAssignment is GuidelineModel.PropertySimpleAssignment psa)
                    {
                        min = psa.Min;
                        max = psa.Max;
                    }
                    else if (property is GuidelineModel.PropertySimple ps)
                    {
                        min = ps.Min;
                        max = ps.Max;
                    }

                    MetadataProperties.Add(new MetadataProperty
                    {
                        Name = property.Name,
                        Value = storedValue ?? string.Empty,
                        StorageType = storageType,
                        PropertyType = propertyType,
                        EnumValues = enumValues,
                        Min = min,
                        Max = max,
                        PropertySetName = classificationProperty.PropertySet?.Name ?? string.Empty,
                        Id = accessRight.GuidlineClassificationPropertyId ?? string.Empty,
                        IsReadOnly = accessRight.Right == PropertyRight.Read
                    });
                }
            }

            instanceData.Add(new InstanceData
            {
                Metadata = singleMetadata,
                MetadataProperties = MetadataProperties,
                ClassificationName = matchingClassification?.Name ?? string.Empty,
            });
        }

        GetInstancesMetadataResponse response = new()
        {
            InstanceData = instanceData
        };

        return response;
    }
}
