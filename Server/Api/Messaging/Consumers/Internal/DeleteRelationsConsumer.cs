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
    /// Represents a consumer for deleting relations of an instance.
    /// </summary>
    public class DeleteRelationsConsumer : IInternalConsumer<DeleteRelations>
    {
        public ILogger<IInternalConsumer<DeleteRelations>> Logger { get; }
        private readonly IInstanceRepository _repository;
        private readonly IAccessRightsFetcher _accessRightsFetcher;
        private readonly IUserGroupProvider _userGroupProvider;
        private readonly IAccessRightHelper _accessRightHelper;
        private readonly ICompletenessCheckScheduler _completenessCheckScheduler;
        private readonly IGraphChangeNotifier _graphChangeNotifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteRelationsConsumer"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="repository">The instance repository.</param>
        /// <param name="accessRightsFetcher">The accessRightsFetcher.</param>
        /// <param name="userGroupProvider">The userGroupProvider.</param>
        /// <param name="accessRightHelper">The accessRightHelper.</param>
        /// <param name="completenessCheckScheduler">The completeness check scheduler.</param>
        /// <param name="graphChangeNotifier">The graph change notifier.</param>
        public DeleteRelationsConsumer(
            ILogger<IInternalConsumer<DeleteRelations>> logger,
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
        /// Consumes the delete relations request.
        /// </summary>
        /// <param name="request">The delete relations request.</param>
        public async Task ConsumeInternal(DeleteRelations request)
        {
            // Fetch metadata for the first instance to get its classification ID
            var metadataResponse1 = await _repository.GetInstance(request.InstanceId);
            if (metadataResponse1 == null)
            {
                throw new InvalidOperationException("Instance not found.");
            }

            var classificationId1 = metadataResponse1.ClassificationId;
            var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
            var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);

            // Get all instances
            var instances = await _repository.GetInstances();
            foreach (var instance in instances)
            {
                foreach (var relation in instance.Relations)
                {
                    var subjectId = relation.SubjectId;
                    var objectId = relation.ObjectId;

                    // Check if either subjectId or objectId matches the instanceId
                    if (subjectId == request.InstanceId || objectId == request.InstanceId)
                    {
                        // Fetch the other instance metadata to get the classification ID
                        var otherInstanceId = subjectId == request.InstanceId ? objectId : subjectId;
                        var otherInstanceMetadata = await _repository.GetInstance(otherInstanceId);

                        if (otherInstanceMetadata == null)
                        {
                            continue;   //TODO: there are always relations that just have the subject be itself and that's it? we use this to skip these
                        }

                        var otherClassificationId = otherInstanceMetadata.ClassificationId;

                        // Check if the user can delete the relation
                        var canDeleteRelations = _accessRightHelper.CanDeleteRelations(classificationId1, otherClassificationId, groupIds, accessRights, request.useCaseId);

                        if (!canDeleteRelations)
                        {
                            continue;       //TODO: here we once again prevent the program from prematurely exiting as we can still delete some of the nodes, but there is no feedback for that case
                        }

                        await _repository.DeleteRelation(subjectId, objectId, relation.PredicateUri);
                        _completenessCheckScheduler.Schedule([subjectId, objectId]);

                        // Inside the loop, so nothing is announced when the user was allowed to
                        // delete none of the relations. Announcing the same use case repeatedly
                        // costs nothing: the notifier collapses what is queued together.
                        _graphChangeNotifier.NotifyChanged(request.useCaseId);
                    }
                }
            }
        }
    }
}
