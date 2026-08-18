// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Models.Enum;

/// <summary>
/// Specifies the direction of a relationship relative to the instance being created.
/// It determines whether the new instance is the source (Subject) or target (Object) of the relationship.
/// </summary>
public enum Direction
{
    /// <summary>
    /// The relationship is from an existing instance (Subject) to the new instance (Object).
    /// </summary>
    From,

    /// <summary>
    /// The relationship is from the new instance (Subject) to an existing instance (Object).
    /// </summary>
    To
}
