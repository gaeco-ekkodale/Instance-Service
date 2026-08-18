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
/// Represents a request to retrieve a specific classification.
/// </summary>
public class GetClassification : IRequest<Classification>
{
    /// <summary>
    /// Gets or sets the unique identifier for the classification to be retrieved.
    /// </summary>
    public string ClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the use case for which the classification is requested.
    /// </summary>
    public string UseCaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication token to authorize the request.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
