// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Dto;
using InstanceService.Models;

using Messaging.Core.Abstractions;
using static InstanceService.Api.Dto.Metadata;

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts;

/// <summary>
/// Represents a request to get node metadata.
/// </summary>
public class GetInstancesMetadataRequest : IRequest<GetInstancesMetadataResponse>
{
    /// <summary>
    /// Gets or sets the instance IDs.
    /// </summary>
    public IEnumerable<string> InstanceIds { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets the use case ID.
    /// </summary>
    public string UseCaseId { get; set; } = string.Empty;

    /// <summary>
    /// The Token of the request.
    /// </summary>
    public string Token {  get; set; } = string.Empty;
}

/// <summary>
/// Represents a response containing the metadata of multiple nodes.
/// </summary>
public class GetInstancesMetadataResponse
{
    public IEnumerable<InstanceData> InstanceData { get; set; } = [];
}

public class InstanceData
{
    /// <summary>
    /// Gets or sets the instance metadata.
    /// </summary>
    public InstanceMetaData Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the metadata properties.
    /// </summary>
    public IEnumerable<MetadataProperty> MetadataProperties { get; set; } = [];

    /// <summary>
    /// Gets or sets the ClassificationName
    /// </summary>
    public string ClassificationName { get; set; } = string.Empty;
}
