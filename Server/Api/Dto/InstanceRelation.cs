// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Dto;

/// <summary>
/// The relation between two nodes with its predicate URI and the label resolved for display.
/// Read model only; use <see cref="Request.CreateRelation"/> to create relations.
/// </summary>
public class InstanceRelation
{
    /// <summary>
    /// The id of the node, which are the subject of the relation.
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The id of the node, which are the object of the relation.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// The canonical ontology property URI identifying the relation.
    /// This is the value used everywhere internally.
    /// </summary>
    public string PredicateUri { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable label of the relation, resolved from the ontology for display only.
    /// Populated by the service on read (GET).
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
