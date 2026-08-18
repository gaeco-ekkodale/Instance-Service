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

namespace InstanceService.Api.Messaging.Consumers.Guidelines.Contracts;

/// <summary>
/// Represents the request to create a reduced guideline.
/// </summary>
public class CreateReducedGuideline : IRequest<CreateReducedGuidelineResponse>
{
    /// <summary>
    /// Gets or sets the ID of the user group.
    /// </summary>
    public string UserGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the use case.
    /// </summary>
    public string UseCaseId { get; set; } = string.Empty;
}

/// <summary>
/// Represents the response for the <see cref="CreateReducedGuideline"/> request.
/// </summary>
public class CreateReducedGuidelineResponse
{
    /// <summary>
    /// Gets or sets the URL of the created guideline.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
