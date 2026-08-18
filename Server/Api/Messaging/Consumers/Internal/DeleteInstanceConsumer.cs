// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using MassTransit.Mediator;
using Messaging.Core.Abstractions;
using Messaging.Core.Extensions.Mediator;

namespace InstanceService.Api.Messaging.Consumers.Internal;

/// <summary>
/// Represents a consumer for deleting an instance.
/// </summary>
public class DeleteInstanceConsumer : IInternalConsumer<DeleteInstance>
{
    public ILogger<IInternalConsumer<DeleteInstance>> Logger { get; }
    private readonly IInstanceRepository _repository;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IUserGroupProvider _userGroupProvider;
    private readonly IAccessRightHelper _accessRightHelper;
    private readonly ICompletenessCheckScheduler _completenessCheckScheduler;
    private readonly IGraphChangeNotifier _graphChangeNotifier;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteInstanceConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repository">The instance repository.</param>
    /// <param name="accessRightsFetcher">The accessRightsFetcher.</param>
    /// <param name="userGroupProvider">The userGroupProvider.</param>
    /// <param name="accessRightHelper">The accessRightHelper.</param>
    /// <param name="completenessCheckScheduler">The completeness check scheduler.</param>
    /// <param name="graphChangeNotifier">The graph change notifier.</param>
    /// <param name="mediator">The mediator.</param>
    public DeleteInstanceConsumer(
        ILogger<IInternalConsumer<DeleteInstance>> logger,
        IInstanceRepository repository,
        IAccessRightsFetcher accessRightsFetcher,
        IUserGroupProvider userGroupProvider,
        IAccessRightHelper accessRightHelper,
        ICompletenessCheckScheduler completenessCheckScheduler,
        IGraphChangeNotifier graphChangeNotifier,
        IMediator mediator
    )
    {
        _repository = repository;
        Logger = logger;
        _accessRightsFetcher = accessRightsFetcher;
        _userGroupProvider = userGroupProvider;
        _accessRightHelper = accessRightHelper;
        _completenessCheckScheduler = completenessCheckScheduler;
        _graphChangeNotifier = graphChangeNotifier;
        _mediator = mediator;
    }

    /// <summary>
    /// Consumes the delete instance request.
    /// </summary>
    /// <param name="request">The delete instance request.</param>
    public async Task ConsumeInternal(DeleteInstance request)
    {
        var classificationId = request.classificationId;
        var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
        var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);

        var canDelete = _accessRightHelper.CanDelete(classificationId, groupIds, accessRights, request.UseCaseId);

        if (!canDelete)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this instance.");
        }

        // Fetch resolved metadata to check read-only property values.
        // instance.Properties uses property names as keys, while AccessRight uses property IDs —
        // GetInstanceMetadataRequest already resolves this mapping correctly.
        var metadata = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
            new GetInstanceMetadataRequest
            {
                InstanceId = request.Id,
                UseCaseId = request.UseCaseId,
                Token = request.Token,
            });

        var hasFilledReadOnlyProperty = metadata.MetadataProperties
            .Any(p => p.IsReadOnly && !string.IsNullOrEmpty(p.Value));

        if (hasFilledReadOnlyProperty)
        {
            throw new UnauthorizedAccessException("Cannot delete instance: read-only properties contain data.");
        }

        await _repository.DeleteInstance(request.Id);
        _completenessCheckScheduler.Schedule(request.Id);
        _graphChangeNotifier.NotifyChanged(request.UseCaseId);
    }
}
