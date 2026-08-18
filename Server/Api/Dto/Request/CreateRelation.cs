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

namespace InstanceService.Api.Dto.Request;

/// <summary>
/// Data transfer object for creating a relation.
/// The relation is identified by its predicate URI; labels are never sent by the client.
/// </summary>
public class CreateRelation
{
    /// <summary>
    /// The id of the subject node of the relation.
    /// </summary>
    [Required]
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The id of the object node of the relation.
    /// </summary>
    [Required]
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// The canonical ontology property URI identifying the relation
    /// (e.g. https://ibpdi.org/ontology/2.0/addressHasBuilding).
    /// </summary>
    [Required]
    public string PredicateUri { get; set; } = string.Empty;

}
