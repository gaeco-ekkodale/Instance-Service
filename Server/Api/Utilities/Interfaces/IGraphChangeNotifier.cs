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
/// Announces that the instances of a use case changed, so that the clients looking at it
/// refetch the graph.
/// </summary>
public interface IGraphChangeNotifier
{
    /// <summary>
    /// Queues a notification for one use case. Does not block and does not send anything
    /// itself, so a failing notification can never fail the write that caused it. Empty IDs
    /// are ignored.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case whose instances changed.</param>
    void NotifyChanged(string? useCaseId);
}
