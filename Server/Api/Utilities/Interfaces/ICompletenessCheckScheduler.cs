// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Utilities.Interfaces;

/// <summary>
/// Queues completeness checks for background processing, so that write operations
/// (create/update/delete of instances and relations) do not wait for the graph traversal.
/// </summary>
public interface ICompletenessCheckScheduler
{
    /// <summary>
    /// Queues a completeness check for a single instance. Does not block, ignores empty IDs.
    /// </summary>
    /// <param name="instanceId">The ID of the instance whose subgraph should be checked.</param>
    void Schedule(string? instanceId);

    /// <summary>
    /// Queues completeness checks for multiple instances. Does not block, ignores empty IDs.
    /// </summary>
    /// <param name="instanceIds">The IDs of the instances whose subgraphs should be checked.</param>
    void Schedule(IEnumerable<string?> instanceIds);
}
