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
/// Represents a consumer for updating an instance.
/// </summary>
public class UpdateInstanceConsumer : IInternalConsumer<UpdateInstance>
{
    public ILogger<IInternalConsumer<UpdateInstance>> Logger { get; }
    private readonly IInstanceRepository _repository;
    private readonly IMediator _mediator;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IUserGroupProvider _userGroupProvider;
    private readonly IAccessRightHelper _accessRightHelper;
    private readonly ICompletenessCheckScheduler _completenessCheckScheduler;
    private readonly IGraphChangeNotifier _graphChangeNotifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateInstanceConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repository">The instance repository.</param>
    /// <param name="accessRightsFetcher">The accessRightsFetcher.</param>
    /// <param name="userGroupProvider">The userGroupProvider.</param>
    /// <param name="accessRightHelper">The accessRightHelper.</param>
    /// <param name="mediator">The mediator.</param>
    /// <param name="completenessCheckScheduler">The completeness check scheduler.</param>
    /// <param name="graphChangeNotifier">The graph change notifier.</param>
    public UpdateInstanceConsumer(
        ILogger<IInternalConsumer<UpdateInstance>> logger,
        IInstanceRepository repository,
        IMediator mediator,
        IAccessRightsFetcher accessRightsFetcher,
        IUserGroupProvider userGroupProvider,
        IAccessRightHelper accessRightHelper,
        ICompletenessCheckScheduler completenessCheckScheduler,
        IGraphChangeNotifier graphChangeNotifier
    )
    {
        _repository = repository;
        Logger = logger;
        _mediator = mediator;
        _accessRightsFetcher = accessRightsFetcher;
        _userGroupProvider = userGroupProvider;
        _accessRightHelper = accessRightHelper;
        _completenessCheckScheduler = completenessCheckScheduler;
        _graphChangeNotifier = graphChangeNotifier;
    }

    /// <summary>
    /// Consumes the update instance request.
    /// </summary>
    /// <param name="request">The update instance request.</param>
    public async Task ConsumeInternal(UpdateInstance request)
    {
        var useCaseId = request.UseCaseId;
        var classificationId = request.ClassificationId;
        var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
        var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);

        var classification = await _mediator.SendInternalRequest<GetClassification, InstanceService.Models.Classification>(new GetClassification()
        {
            ClassificationId = classificationId,
            UseCaseId = useCaseId,
            Token = request.Token,
        });

        var updatableProperties = new Dictionary<string, string>();
        if (request.Properties.Count > 0)
        {
            foreach (var property in request.Properties)
            {
                var classificationProperty = classification.PropertySets
                    .SelectMany(ps => ps.Properties)
                    .FirstOrDefault(p => p.Name.Equals(property.Key, StringComparison.OrdinalIgnoreCase) || p.Id.Equals(property.Key, StringComparison.OrdinalIgnoreCase));

                var canUpdate = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, classificationProperty.Id);
                if (canUpdate)
                {
                    updatableProperties[property.Key] = property.Value;
                }
            }

            if (!updatableProperties.Any())
            {
                throw new UnauthorizedAccessException("You do not have permission to update any properties of this instance.");
            }
        }

        await _repository.UpdateInstance(request.InstanceId, request.Name, updatableProperties);
        _completenessCheckScheduler.Schedule(request.InstanceId);
        _graphChangeNotifier.NotifyChanged(useCaseId);
    }
}
