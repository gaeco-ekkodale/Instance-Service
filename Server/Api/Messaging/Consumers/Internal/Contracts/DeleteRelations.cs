// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts
{
    public class DeleteRelations : IEvent
    {
        /// <summary>
        /// The ids of the instance, whose relations are to be deleted.
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// The token of the request.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The corresponding use case.
        /// </summary>
        public string useCaseId { get; set; } = string.Empty;

        /// <summary>
        /// The Id of the classification, whose relations are to be deleted.
        /// </summary>
        public string classificationId {  get; set; } = string.Empty;
    }
}
