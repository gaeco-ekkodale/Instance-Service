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

namespace InstanceService.Api.Messaging.Consumers.Internal.Contracts;

public class UpdateInstance : IEvent
{
    /// <summary>
    /// The id of the instance.
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The key value pairs that should be added as attributes to the node.
    /// </summary>
    /// <remarks>
    /// Key: the id of the property of a guideline.
    /// Value: the value of the property.
    /// </remarks>
    public Dictionary<string, string> Properties { get; set; } = [];

    /// <summary>
    /// The token of the reqeuest.
    /// </summary>
    public string Token {  get; set; } = string.Empty;

    /// <summary>
    /// The corresponding use case.
    /// </summary>
    public string UseCaseId {  get; set; } = string.Empty;

    /// <summary>
    /// The Id of the classification belonging to the instance to be updated.
    /// </summary>
    public string ClassificationId {  get; set; } = string.Empty;
}
