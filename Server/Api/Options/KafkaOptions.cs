// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;

namespace InstanceService.Api.Options;

/// <summary>
/// Broker-level Kafka configuration shared by every consumer and producer.
/// Per-consumer topic/group settings live under <see cref="Consumers"/> — we run against a single
/// broker but consume from several topics.
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// The name of the configuration section for Kafka settings.
    /// </summary>
    public const string Kafka = "Kafka";

    /// <summary>
    /// Gets or sets the Kafka broker address (shared by all consumers and producers).
    /// </summary>
    [Required]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the auto offset reset policy. Defaults to
    /// <see cref="Confluent.Kafka.AutoOffsetReset.Earliest"/>.
    /// </summary>
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;

    /// <summary>
    /// Gets or sets the session timeout in milliseconds. Defaults to 150000 ms.
    /// </summary>
    public int SessionTimeoutMs { get; set; } = 150000;

    /// <summary>
    /// Gets or sets the maximum poll interval in milliseconds. Defaults to 150000 ms.
    /// </summary>
    public int MaxPollIntervalMs { get; set; } = 150000;

    /// <summary>
    /// Gets or sets the maximum message size in bytes for both producer and consumer.
    /// Defaults to 20971520 bytes (20 MB). Must be aligned with the broker's message.max.bytes setting.
    /// </summary>
    public int MessageMaxBytes { get; set; } = 20971520;

    /// <summary>
    /// Gets or sets the per-consumer settings, keyed by logical consumer name
    /// (e.g. "GraphDataModel", "UseCaseGuidelines", "Ontology").
    /// </summary>
    public Dictionary<string, KafkaConsumerOptions> Consumers { get; set; } = new();

    /// <summary>
    /// Resolves and validates the consumer settings for the given key.
    /// </summary>
    /// <param name="key">The logical consumer key (matches a child of the <c>Kafka:Consumers</c> section).</param>
    /// <exception cref="InvalidOperationException">Thrown when the key is missing or the entry is invalid.</exception>
    public KafkaConsumerOptions GetConsumer(string key)
    {
        if (!Consumers.TryGetValue(key, out var consumer))
            throw new InvalidOperationException(
                $"Missing Kafka consumer configuration for '{key}' (expected under '{Kafka}:{nameof(Consumers)}:{key}').");

        consumer.Validate(key);
        return consumer;
    }
}
