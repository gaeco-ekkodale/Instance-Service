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
using System.Text.RegularExpressions;

namespace InstanceService.Api.Services;

/// <summary>
/// Abstract base class for Kafka consumers running as background services.
/// Handles consumer configuration, topic discovery/subscription, the consume loop and offset commits.
/// Derived classes provide their logical consumer key (resolved against <c>Kafka:Consumers</c>) and
/// implement <see cref="ProcessMessage(string, CancellationToken)"/>.
/// </summary>
public abstract class KafkaConsumerBase : BackgroundService
{
    protected readonly ILogger _logger;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly KafkaOptions _broker;
    protected readonly KafkaConsumerOptions _consumer;
    private readonly Regex? _topicRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumerBase"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">Service provider for resolving scoped dependencies during processing.</param>
    /// <param name="kafkaOptions">Broker-level Kafka options.</param>
    /// <param name="consumerKey">The logical consumer key under <c>Kafka:Consumers</c> (e.g. "Ontology").</param>
    protected KafkaConsumerBase(
        ILogger logger,
        IServiceProvider serviceProvider,
        IOptions<KafkaOptions> kafkaOptions,
        string consumerKey)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _broker = kafkaOptions.Value;
        _consumer = _broker.GetConsumer(consumerKey);
        _topicRegex = _consumer.TopicPattern is { } pattern
            ? new Regex(pattern, RegexOptions.Compiled)
            : null;
    }

    /// <summary>
    /// Name used in log messages. Defaults to the concrete type name.
    /// </summary>
    protected virtual string ConsumerName => GetType().Name;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so StartAsync returns and the host can start subsequent hosted services.
        // The blocking Consume call then runs on a thread-pool thread (see ConsumeMessages).
        await Task.Yield();

        _logger.LogInformation("{Consumer} starting, target: {Target}",
            ConsumerName, _consumer.Topic ?? _consumer.TopicPattern);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Consumer} crashed, restarting in 30s", ConsumerName);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _broker.Address,
            GroupId = _consumer.ConsumerGroup,
            AutoOffsetReset = _broker.AutoOffsetReset,
            SessionTimeoutMs = _broker.SessionTimeoutMs,
            MaxPollIntervalMs = _broker.MaxPollIntervalMs,
            EnableAutoCommit = false,
            MaxPartitionFetchBytes = _broker.MessageMaxBytes,
            FetchMaxBytes = _broker.MessageMaxBytes
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("{Consumer} Kafka error: {Reason}", ConsumerName, e.Reason))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation("{Consumer} assigned partitions: [{Partitions}]",
                    ConsumerName, string.Join(", ", partitions.Select(p => $"{p.Topic}:{p.Partition}"))))
            .Build();

        try
        {
            var topics = await WaitForTopicsAsync(stoppingToken);
            if (topics.Count == 0)
                return; // cancelled while waiting

            consumer.Subscribe(topics);
            _logger.LogInformation("{Consumer} subscribed to [{Topics}]", ConsumerName, string.Join(", ", topics));

            await ConsumeMessages(consumer, stoppingToken);
        }
        finally
        {
            consumer.Close();
        }
    }

    /// <summary>
    /// Polls broker metadata until at least one matching topic exists. A not-yet-created topic
    /// results in a retry instead of a crash.
    /// </summary>
    private async Task<List<string>> WaitForTopicsAsync(CancellationToken stoppingToken)
    {
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _broker.Address }).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var matching = metadata.Topics
                    .Select(t => t.Topic)
                    .Where(IsTargetTopic)
                    .ToList();

                if (matching.Count > 0)
                    return matching;

                _logger.LogWarning("{Consumer} found no topic matching '{Target}' yet, retrying in 30s",
                    ConsumerName, _consumer.Topic ?? _consumer.TopicPattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Consumer} error discovering topics from broker at {Address}, retrying in 30s",
                    ConsumerName, _broker.Address);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        return [];
    }

    private bool IsTargetTopic(string topic) =>
        _topicRegex is not null ? _topicRegex.IsMatch(topic) : topic == _consumer.Topic;

    private async Task ConsumeMessages(IConsumer<Ignore, string> consumer, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Offload the blocking Consume to a thread-pool thread so the async loop stays responsive.
                var result = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);

                if (result?.Message?.Value is null)
                    continue;

                var headers = ExtractHeaders(result.Message.Headers);
                await ProcessMessage(result.Message.Value, headers, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "{Consumer} error consuming message: {Reason}", ConsumerName, ex.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Consumer} unexpected error processing message", ConsumerName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Decodes the Kafka message headers into a case-sensitive string dictionary.
    /// Header values are UTF-8 encoded by the producers (see each service's outbox processor).
    /// </summary>
    private static IReadOnlyDictionary<string, string> ExtractHeaders(Headers? headers)
    {
        if (headers is null || headers.Count == 0)
            return EmptyHeaders;

        var result = new Dictionary<string, string>(headers.Count, StringComparer.Ordinal);
        foreach (var header in headers)
        {
            var bytes = header.GetValueBytes();
            result[header.Key] = bytes is null ? string.Empty : System.Text.Encoding.UTF8.GetString(bytes);
        }
        return result;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
        new Dictionary<string, string>(0);

    /// <summary>
    /// Processes a single consumed Kafka message together with its headers.
    /// On success the offset is committed; throwing skips the commit so the message is retried.
    /// The default implementation ignores the headers and delegates to
    /// <see cref="ProcessMessage(string, CancellationToken)"/>; consumers that need header-based
    /// routing (e.g. an <c>event_type</c> discriminator) override this overload instead.
    /// </summary>
    /// <param name="messageValue">The message value as a string.</param>
    /// <param name="headers">The decoded Kafka message headers (empty if none).</param>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    protected virtual Task ProcessMessage(
        string messageValue, IReadOnlyDictionary<string, string> headers, CancellationToken stoppingToken)
        => ProcessMessage(messageValue, stoppingToken);

    /// <summary>
    /// Processes a single consumed Kafka message. Implemented by consumers that do not need headers.
    /// The base implementation throws so that a consumer overriding only the header-aware overload
    /// is never required to supply a redundant body.
    /// </summary>
    /// <param name="messageValue">The message value as a string.</param>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    protected virtual Task ProcessMessage(string messageValue, CancellationToken stoppingToken)
        => throw new NotImplementedException(
            $"{GetType().Name} must override one of the ProcessMessage overloads.");
}
