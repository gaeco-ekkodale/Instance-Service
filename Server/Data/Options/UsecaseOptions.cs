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

namespace InstanceService.Data.Options;

/// <summary>
/// Represents the configuration options for use case settings.
/// </summary>
public class UsecaseOptions
{
    /// <summary>
    /// The key for the use case configuration section.
    /// </summary>
    public const string UseCase = "UseCase";

    /// <summary>
    /// Gets or sets the network address.
    /// </summary>
    [Required]
    public string Address { get; set; } = string.Empty;
}

