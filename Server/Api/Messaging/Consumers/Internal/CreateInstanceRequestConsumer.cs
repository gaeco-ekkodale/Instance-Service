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
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;

using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Internal;

/// <summary>
/// Represents a consumer for handling internal create node requests.
/// </summary>
public class CreateInstanceRequestConsumer : IInternalRequestConsumer<CreateInstanceRequest, CreateInstanceResponse>
{
    public ILogger<IInternalRequestConsumer<CreateInstanceRequest, CreateInstanceResponse>> Logger { get; }
    private readonly IInstanceRepository _repository;
    private readonly IMapper _mapper;
    private readonly IAccessRightHelper _accessRightHelper;
    private readonly IAccessRightsFetcher _rightsFetcher;
    private readonly IUserGroupProvider _userGroupProvider;
    private readonly ICompletenessCheckScheduler _completenessCheckScheduler;
    private readonly IGraphChangeNotifier _graphChangeNotifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateInstanceRequestConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repository">The instance repository.</param>
    /// <param name="mapper">The automapper.</param>
    /// <param name="accessRightHelper">The accessRight helper.</param>
    /// <param name="rightsFetcher">The access rights fetcher.</param>
    /// <param name="userGroupProvider">The user group provider.</param>
    /// <param name="completenessCheckScheduler">The completeness check scheduler.</param>
    /// <param name="graphChangeNotifier">The graph change notifier.</param>
    public CreateInstanceRequestConsumer(
        ILogger<IInternalRequestConsumer<CreateInstanceRequest, CreateInstanceResponse>> logger,
        IInstanceRepository repository,
        IMapper mapper,
        IAccessRightHelper accessRightHelper,
        IAccessRightsFetcher rightsFetcher,
        IUserGroupProvider userGroupProvider,
        ICompletenessCheckScheduler completenessCheckScheduler,
        IGraphChangeNotifier graphChangeNotifier)
    {
        Logger = logger;
        _repository = repository;
        _mapper = mapper;
        _accessRightHelper = accessRightHelper;
        _rightsFetcher = rightsFetcher;
        _userGroupProvider = userGroupProvider;
        _completenessCheckScheduler = completenessCheckScheduler;
        _graphChangeNotifier = graphChangeNotifier;
    }

    /// <summary>
    /// Consumes the internal create node request.
    /// </summary>
    /// <param name="request">The create node request.</param>
    /// <returns>The create node response.</returns>
    public async Task<CreateInstanceResponse> ConsumeInternal(CreateInstanceRequest request)
    {
        var accessRights = await _rightsFetcher.GetAccessRightsAsync();
        var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);
        var canCreate = _accessRightHelper.CanCreate(request.ClassificationId, groupIds, accessRights, request.useCaseId);

        if (!canCreate)
        {
            throw new UnauthorizedAccessException("You do not have permission to create instances of this type.");
        }

        string id = await _repository.CreateInstance(request.Name, request.ClassificationId, request.Properties);

        // Announced here rather than per return path below: the instance exists from now on,
        // whether or not the relation that may follow it can be created.
        _graphChangeNotifier.NotifyChanged(request.useCaseId);

        CreateInstanceResponse response = new()
        {
            Id = id
        };

        if (request.Relation is null)
        {
            _completenessCheckScheduler.Schedule(id);
            return response;
        }

        InstanceRelation relation = _mapper.Map<InstanceRelation>(request.Relation, opts => opts.Items["Id"] = id);

        await _repository.CreateRelation(relation.SubjectId, relation.ObjectId, relation.PredicateUri);
        _completenessCheckScheduler.Schedule(id);
        return response;
    }
}
