// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace Instance.Events;

/// <summary>
/// Event published by AccessService after uploading a use-case-specific guideline to object storage.
/// </summary>
public class UploadedUseCaseGuideline
{
    /// <summary>
    /// Gets or sets the identifier of the use-case this guideline belongs to.
    /// </summary>
    public string UseCaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object/file name of the guideline in storage.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content hash of the uploaded object for version tracking.
    /// </summary>
    public string Etag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object storage bucket where the guideline was uploaded.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the distributed tracing identifier for correlating related operations.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets when the upload event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
