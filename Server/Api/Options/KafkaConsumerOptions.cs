// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Options;

/// <summary>
/// Per-consumer Kafka settings. All consumers share the broker-level settings on
/// <see cref="KafkaOptions"/>; each entry only supplies its own consumer group and the
/// topic(s) to subscribe to — either a single fixed <see cref="Topic"/> or a
/// <see cref="TopicPattern"/> regex for dynamic discovery (but not both).
/// </summary>
public class KafkaConsumerOptions
{
    /// <summary>
    /// Gets or sets the consumer group ID. Each logical consumer should use its own group
    /// so it tracks offsets independently.
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a single fixed topic to subscribe to. Mutually exclusive with <see cref="TopicPattern"/>.
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Gets or sets a regex pattern used to discover and subscribe to matching topics.
    /// Mutually exclusive with <see cref="Topic"/>.
    /// </summary>
    public string? TopicPattern { get; set; }

    /// <summary>
    /// Validates that the entry is internally consistent.
    /// </summary>
    /// <param name="key">The logical consumer key, used for clear error messages.</param>
    public void Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(ConsumerGroup))
            throw new InvalidOperationException($"Kafka consumer '{key}' is missing a ConsumerGroup.");

        var hasTopic = !string.IsNullOrWhiteSpace(Topic);
        var hasPattern = !string.IsNullOrWhiteSpace(TopicPattern);
        if (hasTopic == hasPattern)
            throw new InvalidOperationException(
                $"Kafka consumer '{key}' must specify exactly one of Topic or TopicPattern.");
    }
}
