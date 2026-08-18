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
using InstanceService.Models;

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts;

/// <summary>
/// Event for producing use case functionality.
/// </summary>
public class UseCaseDataUpdated : IRequest<UseCaseDataUpdatedResponse>
{
    /// <summary>
    /// Name of the usecase for which an event should be produced.
    /// </summary>
    public string UseCaseId { get; set; } = string.Empty;
}

/// <summary>
/// Response for the use case data updated event.
/// </summary>
public class UseCaseDataUpdatedResponse
{
    /// <summary>
    /// Graph data models produced as a result of the use case data updated event.
    /// </summary>
    public List<GraphDataModel> GraphDataModels { get; set; } = [];
}
