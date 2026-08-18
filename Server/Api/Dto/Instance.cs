// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models.Enum;

namespace InstanceService.Api.Dto;

/// <summary>
/// The node of a graph.
/// </summary>
public class Instance
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
    /// The name of the guideline that defines the classification.
    /// </summary>
    public string GuidelineName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates the accessibility of the node.
    /// </summary>
    public Accessibility Accessibility { get; set; } = Accessibility.None;
}
