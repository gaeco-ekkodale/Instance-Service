// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Enums;

namespace InstanceService.Api.Dto;

public class Metadata
{
    /// <summary>
    /// The id of the node.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The id of the classification of the node.
    /// </summary>
    public string ClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the classification of the node.
    /// </summary>
    public string ClassificationName { get; set; } = string.Empty;

    /// <summary>
    /// The properties of the node.
    /// </summary>
    public IEnumerable<MetadataProperty> Properties { get; set; } = [];
}

/// <summary>
/// The property of a metadata object
/// </summary>
public class MetadataProperty
{
    /// <summary>
    /// The id of the property
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the property
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value of the property in an instance
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The name of the property set that the property belongs to
    /// </summary>
    public string PropertySetName { get; set; } = string.Empty;

    /// <summary>
    /// Indicate if the property can be edited or not
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// The storage type of the property
    /// </summary>
    public StorageType StorageType { get; set; } = StorageType.String;

    /// <summary>
    /// Discriminator for the form input widget: "PropertySimple", "PropertyEnum", "PropertySuperEnum", "PropertyTree"
    /// </summary>
    public string PropertyType { get; set; } = "PropertySimple";

    /// <summary>
    /// Available enum options for PropertyEnum and PropertySuperEnum properties
    /// </summary>
    public IEnumerable<MetadataPropertyEnumValue> EnumValues { get; set; } = [];

    /// <summary>
    /// Lower bound constraint for PropertySimple (nullable)
    /// </summary>
    public string? Min { get; set; }

    /// <summary>
    /// Upper bound constraint for PropertySimple (nullable)
    /// </summary>
    public string? Max { get; set; }
}

public class MetadataPropertyEnumValue
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
