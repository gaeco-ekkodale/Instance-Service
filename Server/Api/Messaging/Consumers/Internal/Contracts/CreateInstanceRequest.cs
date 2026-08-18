// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models.Enum;

using Messaging.Core.Abstractions;
using static InstanceService.Api.Dto.Request.CreateInstance;

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts;

/// <summary>
/// Represents a request to create a node.
/// </summary>
public class CreateInstanceRequest : IRequest<CreateInstanceResponse>
{
    /// <summary>
    /// The name the node should have.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The classification id the node should have.
    /// </summary>
    public string ClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// The key value pairs that should be added as attributes to the node.
    /// </summary>
    /// <remarks>
    /// Key: the id of the property of a guideline.
    /// Value: the value of the property.
    /// </remarks>
    public Dictionary<string, string> Properties { get; set; } = [];

    /// <summary>
    /// Optional initial relation of the created node.
    /// </summary>
    public CreateInstanceWithRelation? Relation { get; set; }

    /// <summary>
    /// The use case to create the relation for.
    /// </summary>
    public string useCaseId { get; set; } = string.Empty;

    /// <summary>
    /// The token passed alongside the request.
    /// </summary>
    public string Token {  get; set; } = string.Empty;

    /// <summary>
    /// The data of the initial relation for a create node request.
    /// </summary>
    public class CreateInstanceWithRelation
    {
        /// <summary>
        /// The canonical ontology property URI identifying the relation.
        /// </summary>
        public string PredicateUri { get; set; } = string.Empty;

        /// <summary>
        /// The node id of the other node in the relation.
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// The direction described from the perspective of the node to be created.
        /// </summary>
        public Direction Direction { get; set; } = Direction.From;
    }
}

/// <summary>
/// Represents a response from creating a node.
/// </summary>
public class CreateInstanceResponse
{
    /// <summary>
    /// The id of the created node.
    /// </summary>
    public string Id { get; set; } = "";
}
