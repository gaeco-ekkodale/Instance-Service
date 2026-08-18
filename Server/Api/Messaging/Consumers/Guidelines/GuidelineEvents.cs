// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Messaging.Consumers.Guidelines;

// Field names must match the JSON produced by GuidelineService's events
// (serialized with default System.Text.Json options, i.e. PascalCase). These contracts are
// copy-defined per service — the same pattern AccessService uses to consume the guideline topic.

/// <summary>
/// Event published by the GuidelineService when a guideline file is uploaded or replaced.
/// </summary>
public sealed record UploadedGuideline
{
    /// <summary>The GuidelineService's stable internal ID for the guideline.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>User-friendly display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Object key (path) of the guideline file in object storage.</summary>
    public string ObjectKey { get; init; } = string.Empty;

    /// <summary>ETag (version identifier) of the uploaded object.</summary>
    public string Etag { get; init; } = string.Empty;

    /// <summary>Storage bucket name.</summary>
    public string BucketName { get; init; } = string.Empty;

    /// <summary>Correlation ID for end-to-end tracing.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>UTC timestamp when the upload completed.</summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Event published by the GuidelineService when a guideline is deleted.
/// </summary>
public sealed record DeletedGuideline
{
    /// <summary>The GuidelineService's stable internal ID for the deleted guideline.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Original storage bucket.</summary>
    public string BucketName { get; init; } = string.Empty;

    /// <summary>Original object key (path).</summary>
    public string ObjectKey { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the deletion occurred.</summary>
    public DateTimeOffset Timestamp { get; init; }
}
