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
/// Represents a directed relationship between two instances.
/// </summary>
public class InstanceRelation
{
    /// <summary>
    /// The id of the subject node of the relation.
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The id of the object node of the relation.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// The canonical ontology property URI identifying the relation
    /// (e.g. https://ibpdi.org/ontology/2.0/addressHasBuilding).
    /// This is the identifier used everywhere internally, in the graph DB, and in Kafka export/import.
    /// Human-readable labels are resolved from the ontology for display only.
    /// </summary>
    public string PredicateUri { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable label of the relation, resolved from the ontology by its
    /// <see cref="PredicateUri"/>. Populated for display when a graph is read; may be null when
    /// unknown or not yet resolved. Not part of the relation's identity
    /// (<see cref="Equals"/>/<see cref="GetHashCode"/> use only subject, object and predicate URI).
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance relation.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance relation.</param>
    /// <returns> <c>true</c> if the specified object is an <see cref="InstanceRelation"/> and has the same subject ID, object ID, and predicate URI; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is InstanceRelation other)
        {
            return SubjectId == other.SubjectId && ObjectId == other.ObjectId && PredicateUri == other.PredicateUri;
        }
        return false;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object, based on the combination of <see cref="SubjectId"/>, <see cref="ObjectId"/>, and <see cref="PredicateUri"/>.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(SubjectId, ObjectId, PredicateUri);
    }
}
