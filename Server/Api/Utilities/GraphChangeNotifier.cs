// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Utilities.Interfaces;
using System.Threading.Channels;

namespace InstanceService.Api.Utilities;

/// <summary>
/// Unbounded in-memory queue of use case IDs whose clients are waiting to be notified.
/// Registered as a singleton, drained by <see cref="Services.GraphChangeBroadcastWorker"/>.
/// </summary>
/// <remarks>
/// Queued notifications only live in memory. A restart drops them, which costs the connected
/// clients one refresh - they refetch on their next change or when the tab is focused again.
/// </remarks>
public class GraphChangeNotifier : IGraphChangeNotifier
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    /// <summary>
    /// The reader side of the queue, consumed by the background worker.
    /// </summary>
    public ChannelReader<string> Reader => _queue.Reader;

    /// <inheritdoc />
    public void NotifyChanged(string? useCaseId)
    {
        if (string.IsNullOrEmpty(useCaseId))
            return;

        _queue.Writer.TryWrite(useCaseId);
    }
}
