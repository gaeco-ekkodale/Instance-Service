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
/// Represents an use-case.
/// Originates from UseCase.Data.
/// </summary>
public class UseCase
{
    /// <summary>
    /// Primary identifier for the use-case.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The title of the use-case.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the use-case.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

