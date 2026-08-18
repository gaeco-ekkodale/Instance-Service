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
/// Unbounded in-memory queue of instance IDs awaiting a completeness check.
/// Registered as a singleton, drained by <see cref="Services.CompletenessCheckWorker"/>.
/// </summary>
/// <remarks>
/// Pending work only lives in memory. A restart drops it, which is acceptable because the
/// check is idempotent and the next change to the subgraph triggers it again.
/// </remarks>
public class CompletenessCheckScheduler : ICompletenessCheckScheduler
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
    public void Schedule(string? instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return;

        _queue.Writer.TryWrite(instanceId);
    }

    /// <inheritdoc />
    public void Schedule(IEnumerable<string?> instanceIds)
    {
        foreach (var instanceId in instanceIds)
        {
            Schedule(instanceId);
        }
    }
}
