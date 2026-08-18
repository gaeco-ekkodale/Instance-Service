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
using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Internal
{
    /// <summary>
    /// Represents a consumer for deleting a relation of an instance.
    /// </summary>
    public class DeleteRelationConsumer : IInternalConsumer<DeleteRelation>
    {
        public ILogger<IInternalConsumer<DeleteRelation>> Logger { get; }
        private readonly IInstanceRepository _repository;
        private readonly IAccessRightsFetcher _accessRightsFetcher;
        private readonly IUserGroupProvider _userGroupProvider;
        private readonly IAccessRightHelper _accessRightHelper;
        private readonly ICompletenessCheckScheduler _completenessCheckScheduler;
        private readonly IGraphChangeNotifier _graphChangeNotifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteRelationConsumer"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="repository">The instance repository.</param>
        /// <param name="accessRightsFetcher">The Access Rights provider.</param>
        /// <param name="userGroupProvider">The User Group provider.</param>
        /// <param name="accessRightHelper">The accessRight helper.</param>
        /// <param name="completenessCheckScheduler">The completeness check scheduler.</param>
        /// <param name="graphChangeNotifier">The graph change notifier.</param>
        public DeleteRelationConsumer(
            ILogger<IInternalConsumer<DeleteRelation>> logger,
            IInstanceRepository repository,
            IAccessRightsFetcher accessRightsFetcher,
            IUserGroupProvider userGroupProvider,
            IAccessRightHelper accessRightHelper,
            ICompletenessCheckScheduler completenessCheckScheduler,
            IGraphChangeNotifier graphChangeNotifier
        )
        {
            _repository = repository;
            Logger = logger;
            _accessRightsFetcher = accessRightsFetcher;
            _userGroupProvider = userGroupProvider;
            _accessRightHelper = accessRightHelper;
            _completenessCheckScheduler = completenessCheckScheduler;
            _graphChangeNotifier = graphChangeNotifier;
        }

        /// <summary>
        /// Consumes the delete relation request.
        /// </summary>
        /// <param name="request">The delete relation request.</param>
        public async Task ConsumeInternal(DeleteRelation request)
        {
            // Fetch metadata for the first instance to get its classification ID
            var metadataResponse1 = await _repository.GetInstance(request.InstanceId);
            if (metadataResponse1 == null)
            {
                throw new InvalidOperationException("Instance not found.");
            }

            var subjectClassificationId = metadataResponse1.ClassificationId;
            var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
            var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);

            // Get all instances
            var instances = await _repository.GetInstances();

            // Checks if a relation with given instance exists and delete this one if the user is allowed to
            var existingInstance = instances.Where(instance => instance.Id == request.InstanceId).FirstOrDefault();
            if (existingInstance == null)
                throw new ArgumentException("A node with the given instanceId doesn't exist.");
            var existingRelation = existingInstance.Relations.Where(relation =>
                    (relation.SubjectId == request.InstanceId)
                    && (relation.PredicateUri == request.PredicateUri)
                    && (relation.ObjectId == request.ObjectId)
                ).FirstOrDefault();

            if (existingRelation == null)
                throw new ArgumentException("A relation with the given parameters doesn't exist.");

            var subjectId = existingRelation.SubjectId;
            var objectId = existingRelation.ObjectId;
            var predicateUri = existingRelation.PredicateUri;

            var otherInstanceMetadata = await _repository.GetInstance(objectId)
                ?? throw new Exception("No metadata found for existing objectId.");

            var objectClassificationId = otherInstanceMetadata.ClassificationId;
            var canDeleteRelations = _accessRightHelper.CanDeleteRelations(subjectClassificationId, objectClassificationId, groupIds, accessRights, request.useCaseId);

            if (!canDeleteRelations)
                throw new ArgumentException("The user is not allowed to delete the requested relation.");

            await _repository.DeleteRelation(subjectId, objectId, predicateUri);

            _completenessCheckScheduler.Schedule([subjectId, objectId]);
            _graphChangeNotifier.NotifyChanged(request.useCaseId);
        }
    }
}
