// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;

namespace InstanceService.Models;

/// <summary>
/// Represents a single metadata node with a unique ID, a class type, and a list of properties.
/// </summary>
public class MetaDataNode
{
    /// <summary>
    /// ID for the metadata node. Must be unique.
    /// </summary>
    [Required]
    public string Id { get; set; }

    /// <summary>
    /// Class type for the metadata node. Must be a valid class from the guideline.
    /// </summary>
    [Required]
    public string ClassType { get; set; }

    [Required]
    public string Code { get; set; }

    /// <summary>
    /// Key Value pairs for the metadata for the class type used. Must match the guideline class type properties.
    /// </summary>
    [Required]
    public Dictionary<string, string> PropertiesValues { get; set; }

    public MetaDataNode()
    {
        Id = string.Empty;
        ClassType = string.Empty;
        Code = string.Empty;
        PropertiesValues = new Dictionary<string, string>();
    }
}
