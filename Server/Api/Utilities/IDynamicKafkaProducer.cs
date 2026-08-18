// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Utilities;

/// <summary>
/// Interface for dynamic Kafka message producer that can send messages to topics specified at runtime.
/// </summary>
public interface IDynamicKafkaProducer
{
    /// <summary>
    /// Sends a message to a dynamically specified Kafka topic.
    /// </summary>
    /// <typeparam name="T">The type of the message to send.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="topicName">The name of the Kafka topic to send the message to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProduceToDynamicTopicAsync<T>(T message, string topicName) where T : class;

    /// <summary>
    /// Sends a message to a dynamically specified Kafka topic with custom headers.
    /// </summary>
    /// <typeparam name="T">The type of the message to send.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="topicName">The name of the Kafka topic to send the message to.</param>
    /// <param name="headers">A dictionary of headers to include with the message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProduceToDynamicTopicAsync<T>(T message, string topicName, IDictionary<string, object> headers) where T : class;
}
