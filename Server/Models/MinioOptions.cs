// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstanceService.Models;

/// <summary>
/// Represents the options for connecting to Minio.
/// </summary>
public class MinioOptions
{
    /// <summary>
    /// Gets or sets the address of the Minio server.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access key for authenticating with the Minio server.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key for authenticating with the Minio server.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}
