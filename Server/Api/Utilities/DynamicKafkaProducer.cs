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
using InstanceService.Api.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstanceService.Api.Utilities;

/// <summary>
/// Provides a mechanism to produce messages to Kafka topics that are determined at runtime.
/// </summary>
/// <remarks>
/// This class is a wrapper around the Confluent.Kafka <see cref="IProducer{TKey, TValue}"/> to simplify message production and centralize configuration.
/// It implements <see cref="IDisposable"/> to ensure that the underlying producer is properly disposed of.
/// </remarks>
public class DynamicKafkaProducer : IDynamicKafkaProducer, IDisposable
{
    private readonly ILogger<DynamicKafkaProducer> _logger;
    private readonly IProducer<Null, string> _producer;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicKafkaProducer"/> class.
    /// </summary>
    /// <param name="logger">The logger for logging information and errors.</param>
    /// <param name="kafkaOptions">The Kafka configuration options, injected via IOptions.</param>
    public DynamicKafkaProducer(ILogger<DynamicKafkaProducer> logger, IOptions<KafkaOptions> kafkaOptions)
    {
        _logger = logger;
        
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.Address,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            EnableIdempotence = true,
            MessageMaxBytes = kafkaOptions.Value.MessageMaxBytes
        };

        _producer = new ProducerBuilder<Null, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e.Reason))
            .Build();

        // Configure ReferenceHandler.Preserve to serialize complex nested structures (e.g., Guidelines with Domain/Classifications).
        // This ensures $id, $ref, $type, and $values metadata is preserved in Kafka messages for proper deserialization.
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve
        };
    }

    /// <inheritdoc />
    public async Task ProduceToDynamicTopicAsync<T>(T message, string topicName) where T : class
    {
        await ProduceToDynamicTopicAsync(message, topicName, new Dictionary<string, object>());
    }

    /// <inheritdoc />
    public async Task ProduceToDynamicTopicAsync<T>(T message, string topicName, IDictionary<string, object> headers) where T : class
    {
        try
        {
            // Serialize message using configured options that preserve references and type information.
            var messageValue = JsonSerializer.Serialize(message, _jsonSerializerOptions);
            var messageSizeBytes = System.Text.Encoding.UTF8.GetByteCount(messageValue);
            var messageSizeMb = messageSizeBytes / (1024.0 * 1024.0);

            _logger.LogInformation(
                "Kafka message for topic {Topic}: Size = {SizeBytes} bytes ({SizeMB:F2} MB)",
                topicName, messageSizeBytes, messageSizeMb);

            var kafkaMessage = new Message<Null, string>
            {
                Value = messageValue,
                Headers = new Headers()
            };

            // Add custom headers
            foreach (var header in headers)
            {
                var headerValue = header.Value switch
                {
                    string s => System.Text.Encoding.UTF8.GetBytes(s),
                    byte[] b => b,
                    _ => System.Text.Encoding.UTF8.GetBytes(header.Value?.ToString() ?? "")
                };
                
                kafkaMessage.Headers.Add(header.Key, headerValue);
            }

            var result = await _producer.ProduceAsync(topicName, kafkaMessage);
            
            _logger.LogInformation(
                "Message sent to topic {Topic} at partition {Partition}, offset {Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}: {Error}", topicName, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing message to topic {Topic}", topicName);
            throw;
        }
    }

    /// <summary>
    /// Releases the resources used by the Kafka producer.
    /// </summary>
    public void Dispose()
    {
        _producer?.Dispose();
    }
}
