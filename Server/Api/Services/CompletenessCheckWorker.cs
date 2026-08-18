// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;
using System.Diagnostics;

namespace InstanceService.Api.Services;

/// <summary>
/// Runs the queued completeness checks outside of the request and consume path.
/// </summary>
/// <remarks>
/// Scheduled IDs are collected into batches: consecutive changes usually affect the same
/// subgraph, and <see cref="ICompletenessCheck.CheckAndSendAsync(string[])"/> sends such a
/// subgraph once per batch instead of once per touched instance.
/// </remarks>
public class CompletenessCheckWorker : BackgroundService
{
    /// <summary>How long to wait for further IDs before starting a batch.</summary>
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>Upper bound for collecting a batch, so a continuous stream of changes still gets checked.</summary>
    private static readonly TimeSpan MaxBatchWindow = TimeSpan.FromSeconds(5);

    private readonly CompletenessCheckScheduler _scheduler;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompletenessCheckWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompletenessCheckWorker"/> class.
    /// </summary>
    /// <param name="scheduler">The queue to drain.</param>
    /// <param name="scopeFactory">Factory used to resolve the scoped completeness check per batch.</param>
    /// <param name="logger">The logger.</param>
    public CompletenessCheckWorker(
        CompletenessCheckScheduler scheduler,
        IServiceScopeFactory scopeFactory,
        ILogger<CompletenessCheckWorker> logger)
    {
        _scheduler = scheduler;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Reads scheduled instance IDs and processes them in batches until shutdown.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token signalled on shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Completeness check worker started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var firstInstanceId = await _scheduler.Reader.ReadAsync(stoppingToken);
                var batch = await CollectBatchAsync(firstInstanceId, stoppingToken);

                await RunBatchAsync(batch, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown.
        }

        _logger.LogInformation("Completeness check worker stopped");
    }

    /// <summary>
    /// Collects the instance IDs that belong to the same burst of changes.
    /// </summary>
    /// <param name="firstInstanceId">The ID that opened the batch.</param>
    /// <param name="stoppingToken">Cancellation token signalled on shutdown.</param>
    /// <returns>The distinct instance IDs to check.</returns>
    private async Task<HashSet<string>> CollectBatchAsync(string firstInstanceId, CancellationToken stoppingToken)
    {
        var batch = new HashSet<string>(StringComparer.Ordinal) { firstInstanceId };
        var window = Stopwatch.StartNew();

        while (window.Elapsed < MaxBatchWindow)
        {
            await Task.Delay(DebounceDelay, stoppingToken);

            var received = 0;
            while (_scheduler.Reader.TryRead(out var instanceId))
            {
                batch.Add(instanceId);
                received++;
            }

            // Nothing arrived while waiting, so the burst is over.
            if (received == 0)
                break;
        }

        return batch;
    }

    /// <summary>
    /// Runs the completeness check for one batch in its own DI scope. Failures are logged,
    /// the affected subgraph is checked again on its next change.
    /// </summary>
    /// <param name="instanceIds">The instance IDs to check.</param>
    /// <param name="stoppingToken">Cancellation token signalled on shutdown.</param>
    private async Task RunBatchAsync(HashSet<string> instanceIds, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        var duration = Stopwatch.StartNew();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var completenessCheck = scope.ServiceProvider.GetRequiredService<ICompletenessCheck>();

            await completenessCheck.CheckAndSendAsync(instanceIds.ToArray());

            _logger.LogInformation("Checked {Count} instance(s) in {ElapsedMilliseconds} ms",
                instanceIds.Count, duration.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Completeness check failed for instances {InstanceIds}",
                string.Join(", ", instanceIds));
        }
    }
}
