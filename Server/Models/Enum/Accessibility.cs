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
/// Specifies the accessibility level for an instance, determined by the available property rights.
/// </summary>
public enum Accessibility
{

    /// <summary>
    /// No access is granted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Read-only access is granted.
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Both read and write access are granted.
    /// </summary>
    ReadWrite = 2,

    /// <summary>
    /// Full access is granted.
    /// </summary>
    FullControl = 3

}
