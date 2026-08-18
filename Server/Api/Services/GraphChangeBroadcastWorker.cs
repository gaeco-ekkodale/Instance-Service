// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Hubs;
using InstanceService.Api.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace InstanceService.Api.Services;

/// <summary>
/// Sends the queued graph change notifications, outside of the request and consume path.
/// </summary>
/// <remarks>
/// Queued IDs are collected over a fixed window, so a use case is announced once per window
/// however many instances were written in it. This keeps a bulk import - which writes one
/// instance per Kafka message - from making every connected client refetch the whole graph
/// hundreds of times.
/// </remarks>
public class GraphChangeBroadcastWorker : BackgroundService
{
    /// <summary>
    /// How long changes are collected before the clients of a use case are told once, and with
    /// that the worst case delay until the change of one user reaches the others.
    /// </summary>
    /// <remarks>
    /// A second is short enough to feel immediate next to the round trip that follows it, and
    /// long enough to fold a bulk import into a handful of announcements instead of one per
    /// written instance.
    /// </remarks>
    public static readonly TimeSpan BroadcastWindow = TimeSpan.FromSeconds(1);

    private readonly GraphChangeNotifier _notifier;
    private readonly IHubContext<GraphHub> _hub;
    private readonly ILogger<GraphChangeBroadcastWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphChangeBroadcastWorker"/> class.
    /// </summary>
    /// <param name="notifier">The queue to drain.</param>
    /// <param name="hub">The hub used to reach the clients of a use case.</param>
    /// <param name="logger">The logger.</param>
    public GraphChangeBroadcastWorker(
        GraphChangeNotifier notifier,
        IHubContext<GraphHub> hub,
        ILogger<GraphChangeBroadcastWorker> logger)
    {
        _notifier = notifier;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Reads queued use case IDs and notifies their clients until shutdown.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token signalled on shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Graph change broadcast worker started, collecting changes for {WindowMs}ms",
            BroadcastWindow.TotalMilliseconds);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var firstUseCaseId = await _notifier.Reader.ReadAsync(stoppingToken);
                var useCaseIds = await CollectWindowAsync(firstUseCaseId, stoppingToken);

                await BroadcastAsync(useCaseIds, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown.
        }

        _logger.LogInformation("Graph change broadcast worker stopped");
    }

    /// <summary>
    /// Collects the use case IDs queued within one window of the first one.
    /// </summary>
    /// <remarks>
    /// A fixed window rather than a delay that restarts on every change: a use case written to
    /// continuously would otherwise keep postponing its own notification.
    /// </remarks>
    /// <param name="firstUseCaseId">The ID that opened the window.</param>
    /// <param name="stoppingToken">Cancellation token signalled on shutdown.</param>
    /// <returns>The distinct use case IDs to announce.</returns>
    private async Task<HashSet<string>> CollectWindowAsync(string firstUseCaseId, CancellationToken stoppingToken)
    {
        var useCaseIds = new HashSet<string>(StringComparer.Ordinal) { firstUseCaseId };

        await Task.Delay(BroadcastWindow, stoppingToken);

        while (_notifier.Reader.TryRead(out var useCaseId))
        {
            useCaseIds.Add(useCaseId);
        }

        return useCaseIds;
    }

    /// <summary>
    /// Notifies the clients of every collected use case. A failed notification is logged and
    /// dropped: it costs those clients one refresh, and the next change announces the use case
    /// again.
    /// </summary>
    /// <param name="useCaseIds">The use case IDs to announce.</param>
    /// <param name="stoppingToken">Cancellation token signalled on shutdown.</param>
    private async Task BroadcastAsync(HashSet<string> useCaseIds, CancellationToken stoppingToken)
    {
        foreach (var useCaseId in useCaseIds)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                await _hub.Clients
                    .Group(GraphHub.GroupFor(useCaseId))
                    .SendAsync(GraphHub.GraphChangedEvent, useCaseId, stoppingToken);

                _logger.LogDebug("Announced changes of use case {UseCaseId}", useCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to announce changes of use case {UseCaseId}", useCaseId);
            }
        }
    }
}
