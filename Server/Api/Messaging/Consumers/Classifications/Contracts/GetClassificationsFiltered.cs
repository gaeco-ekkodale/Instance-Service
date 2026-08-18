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

namespace InstanceService.Api.Messaging.Consumers.Classifications.Contracts;

/// <summary>
/// Represents a request to retrieve a filtered list of classifications.
/// </summary>
public class GetClassificationsFiltered : IRequest<ClassificationListResponse>
{
    /// <summary>
    /// Gets or sets the identifier for the context from which to retrieve the classifications.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication token to authorize the request.
    /// </summary>
    public string Token {  get; set; } = string.Empty;
}

/// <summary>
/// Represents the response containing a list of classifications.
/// </summary>
public class ClassificationListResponse
{
    /// <summary>
    /// Gets or sets the set of classifications.
    /// </summary>
    public ClassificationsListSet Classifications { get; set; }
}
