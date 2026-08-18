// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Dto.Ontology;
using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Ontology.Contracts
{
    /// <summary>
    /// Represents a response containing relations.
    /// </summary>
    public class GetRelationsResponse
    {
        /// <summary>
        /// Gets or sets the list of relations.
        /// </summary>
        public List<RelationDTO> Relations { get; set; } = [];
    }
}
