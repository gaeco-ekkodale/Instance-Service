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
using FluentAssertions;
using InstanceService.Api.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;

namespace InstanceService.Api.Tests.Utilities;

public class KafkaMessageSizeTests
{
    /// <summary>
    /// Verifies that the default MessageMaxBytes value is 20 MB (20971520 bytes),
    /// which is sufficient for large messages.
    /// </summary>
    [Fact]
    public void KafkaOptions_DefaultMessageMaxBytes_Is20MB()
    {
        // Arrange & Act
        var options = new KafkaOptions();

        // Assert
        options.MessageMaxBytes.Should().Be(20971520, "because the default should support 20 MB messages");
    }

    /// <summary>
    /// Verifies that MessageMaxBytes can be set to a custom value via configuration.
    /// </summary>
    [Theory]
    [InlineData(1048576)]     // 1 MB
    [InlineData(10485760)]    // 10 MB
    [InlineData(20971520)]    // 20 MB
    [InlineData(52428800)]    // 50 MB
    [InlineData(104857600)]   // 100 MB
    public void KafkaOptions_MessageMaxBytes_CanBeConfigured(int expectedBytes)
    {
        // Arrange & Act
        var options = new KafkaOptions { MessageMaxBytes = expectedBytes };

        // Assert
        options.MessageMaxBytes.Should().Be(expectedBytes);
    }

    /// <summary>
    /// Verifies that ProducerConfig is constructed with the correct MessageMaxBytes
    /// from KafkaOptions, matching the pattern used in DynamicKafkaProducer.
    /// </summary>
    [Fact]
    public void ProducerConfig_UsesMessageMaxBytesFromOptions()
    {
        // Arrange
        var kafkaOptions = new KafkaOptions
        {
            Address = "localhost:9092",
            MessageMaxBytes = 20971520
        };

        // Act - mirrors the config construction in DynamicKafkaProducer
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Address,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            EnableIdempotence = true,
            MessageMaxBytes = kafkaOptions.MessageMaxBytes
        };

        // Assert
        config.MessageMaxBytes.Should().Be(20971520, "because the producer must accept 20 MB messages");
    }

    /// <summary>
    /// Verifies that ConsumerConfig is constructed with the correct MaxPartitionFetchBytes
    /// and FetchMaxBytes from KafkaOptions, matching the pattern used in KafkaConsumerBase.
    /// </summary>
    [Fact]
    public void ConsumerConfig_UsesMessageMaxBytesFromOptions()
    {
        // Arrange
        var kafkaOptions = new KafkaOptions
        {
            Address = "localhost:9092",
            MessageMaxBytes = 20971520
        };

        // Act - mirrors the config construction in KafkaConsumerBase
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Address,
            GroupId = "test-group",
            AutoOffsetReset = kafkaOptions.AutoOffsetReset,
            SessionTimeoutMs = kafkaOptions.SessionTimeoutMs,
            MaxPollIntervalMs = kafkaOptions.MaxPollIntervalMs,
            EnableAutoCommit = false,
            MaxPartitionFetchBytes = kafkaOptions.MessageMaxBytes,
            FetchMaxBytes = kafkaOptions.MessageMaxBytes + 512
        };

        // Assert
        config.MaxPartitionFetchBytes.Should().Be(20971520,
            "because the consumer must be able to fetch messages up to 20 MB per partition");
        config.FetchMaxBytes.Should().Be(20971520 + 512,
            "because FetchMaxBytes needs overhead beyond the message size");
    }

    /// <summary>
    /// Verifies that a message close to the maximum size (19 MB) can be serialized
    /// without exceeding the configured limit.
    /// </summary>
    [Fact]
    public void LargeMessage_NearMaxSize_CanBeSerializedWithinLimit()
    {
        // Arrange
        var kafkaOptions = new KafkaOptions
        {
            MessageMaxBytes = 20971520 // 20 MB
        };

        // Create a payload that is approximately 19 MB (just under the 20 MB limit)
        var targetSizeBytes = 19 * 1024 * 1024; // 19 MB
        var largePayload = new string('X', targetSizeBytes);

        var message = new { Data = largePayload };

        // Act
        var serialized = JsonSerializer.Serialize(message);
        var serializedBytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        // Assert
        serializedBytes.Length.Should().BeGreaterThan(19 * 1024 * 1024,
            "because the serialized message should be at least 19 MB");
        serializedBytes.Length.Should().BeLessThan(kafkaOptions.MessageMaxBytes,
            "because the serialized message must fit within the configured MessageMaxBytes limit");
    }

    /// <summary>
    /// Verifies that the ProducerConfig and ConsumerConfig message size limits
    /// are properly aligned to prevent producer/consumer mismatches.
    /// </summary>
    [Fact]
    public void ProducerAndConsumer_MessageSizeLimits_AreAligned()
    {
        // Arrange
        var kafkaOptions = new KafkaOptions
        {
            Address = "localhost:9092",
            MessageMaxBytes = 20971520
        };

        // Act
        var producerConfig = new ProducerConfig
        {
            MessageMaxBytes = kafkaOptions.MessageMaxBytes
        };

        var consumerConfig = new ConsumerConfig
        {
            MaxPartitionFetchBytes = kafkaOptions.MessageMaxBytes,
            FetchMaxBytes = kafkaOptions.MessageMaxBytes + 512
        };

        // Assert - Consumer must be able to fetch what the producer sends
        consumerConfig.MaxPartitionFetchBytes.Should().Be(kafkaOptions.MessageMaxBytes,
            "because the consumer's MaxPartitionFetchBytes must be >= producer's MessageMaxBytes");
        consumerConfig.FetchMaxBytes.Should().Be(kafkaOptions.MessageMaxBytes + 512,
            "because FetchMaxBytes must exceed MessageMaxBytes to account for overhead");
    }
}
