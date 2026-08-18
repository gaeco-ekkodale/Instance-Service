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

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts;

/// <summary>
/// Represents a request to get a graph.
/// </summary>
public class GetGraphRequest : IRequest<GetGraphResponse>
{
    /// <summary>
    /// Gets or sets the use case ID.
    /// </summary>
    public string UseCaseId { get; set; } = "";
}

/// <summary>
/// Represents a response containing a graph.
/// </summary>
public class GetGraphResponse
{
    /// <summary>
    /// The nodes in a graph.
    /// </summary>
    public IEnumerable<(Models.Instance instance, Accessibility accessibility)> Instances { get; set; } = [];

    /// <summary>
    /// The relations in a graph.
    /// </summary>
    public IEnumerable<Models.InstanceRelation> Relations { get; set; } = [];
}
