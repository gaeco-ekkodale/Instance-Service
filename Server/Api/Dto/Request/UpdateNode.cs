// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Dto.Request;

/// <summary>
/// Data transfer object for updating a node.
/// </summary>
public class UpdateInstance
{

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
    public Dictionary<string, string> Properties { get; set; } = new();

}
