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
/// Represents the configuration options for connecting to a Gremlin-compatible database.
/// </summary>
public class GremlinOptions
{
    /// <summary>
    /// The key for the Gremlin configuration section in appsettings.json.
    /// </summary>
    public const string SectionName = "Gremlin";

    /// <summary>
    /// Gets or sets the hostname of the Gremlin server.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string Hostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the port of the Gremlin server.
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Port { get; set; } = 8182;

    /// <summary>
    /// Gets or sets the name of the database to use.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether SSL is enabled for the Gremlin connection.
    /// Defaults to false.
    /// </summary>
    public bool EnableSSL { get; set; } = false;
}