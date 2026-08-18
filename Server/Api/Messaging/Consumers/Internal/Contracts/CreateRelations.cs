// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models;

using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts;

/// <summary>
/// Represents the contract for creating relations.
/// </summary>
public class CreateRelations : IEvent
{
    /// <summary>
    /// Gets or sets the relations to create.
    /// </summary>
    public IEnumerable<InstanceRelation> Relations { get; set; } = [];

    /// <summary>
    /// The token of the request.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The corresponding use case.
    /// </summary>
    public string UseCaseId {  get; set; } = string.Empty;
}
