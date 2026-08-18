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
using Microsoft.IdentityModel.Tokens;

namespace InstanceService.Api.Messaging.Consumers.Internal;

/// <summary>
/// Represents a consumer for creating relations.
/// </summary>
public class CreateRelationsConsumer : IInternalConsumer<CreateRelations>
{
    public ILogger<IInternalConsumer<CreateRelations>> Logger { get; }
    private readonly IInstanceRepository _repository;
    private readonly IMediator _mediator;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IUserGroupProvider _userGroupProvider;
    private readonly IAccessRightHelper _accessRightHelper;
    private readonly ICompletenessCheckScheduler _completenessCheckScheduler;
    private readonly IGraphChangeNotifier _graphChangeNotifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRelationsConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repository">The instance repository.</param>
    /// <param name="accessRightsFetcher">The accessRightsFetcher.</param>
    /// <param name="mediator">The mediator.</param>
    /// <param name="userGroupProvider">The userGroupProvider.</param>
    /// <param name="accessRightHelper">The accessRightHelper.</param>
    /// <param name="completenessCheckScheduler">The completeness check scheduler.</param>
    /// <param name="graphChangeNotifier">The graph change notifier.</param>
    public CreateRelationsConsumer(
        ILogger<IInternalConsumer<CreateRelations>> logger,
        IInstanceRepository repository,
        IMediator mediator,
        IAccessRightsFetcher accessRightsFetcher,
        IUserGroupProvider userGroupProvider,
        IAccessRightHelper accessRightHelper,
        ICompletenessCheckScheduler completenessCheckScheduler,
        IGraphChangeNotifier graphChangeNotifier)
    {
        Logger = logger;
        _repository = repository;
        _mediator = mediator;
        _accessRightsFetcher = accessRightsFetcher;
        _userGroupProvider = userGroupProvider;
        _accessRightHelper = accessRightHelper;
        _completenessCheckScheduler = completenessCheckScheduler;
        _graphChangeNotifier = graphChangeNotifier;
    }

    /// <summary>
    /// Consumes the create relations request.
    /// </summary>
    /// <param name="request">The create relations request.</param>
    public async Task ConsumeInternal(CreateRelations request)
    {
        var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
        var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);

        var validRelations = new List<Models.InstanceRelation>();

        foreach (var relation in request.Relations)
        {
            var metadata1 = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
                new GetInstanceMetadataRequest { InstanceId = relation.SubjectId, UseCaseId = request.UseCaseId, Token = request.Token });

            var metadata2 = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
                new GetInstanceMetadataRequest { InstanceId = relation.ObjectId, UseCaseId = request.UseCaseId, Token = request.Token });

            var canCreateRelation = _accessRightHelper.CanCreateRelations(metadata1.Metadata.ClassificationId, metadata2.Metadata.ClassificationId, groupIds, accessRights, request.UseCaseId);

            if (canCreateRelation)
            {
                validRelations.Add(relation);
            }
            else
            {
                continue;   //TODO; not sure if possible but ideally we want to tell the client that it partially worked, so either a yellow toast or even better 2 toasts (red/green) but that'd require 2 api calls I imagine?
            }
        }

        if (validRelations.IsNullOrEmpty())
        {
            throw new UnauthorizedAccessException("You do not have permission to create any of the selected relations.");
        }

        await _repository.CreateRelations(validRelations);

        _completenessCheckScheduler.Schedule(validRelations.Select(relation => relation.SubjectId));
        _graphChangeNotifier.NotifyChanged(request.UseCaseId);
    }
}
