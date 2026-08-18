// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Models;

/// <summary>
/// Represents an instance with its properties and relations.
/// </summary>
public class Instance
{

    /// <summary>
    /// Gets or sets the unique identifier for the instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the instance.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the classification to which this instance belongs.
    /// </summary>
    public string ClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the properties of the instance as a key-value pair dictionary.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of relations to other instances.
    /// </summary>
    public List<InstanceRelation> Relations { get; set; } = [];

}
