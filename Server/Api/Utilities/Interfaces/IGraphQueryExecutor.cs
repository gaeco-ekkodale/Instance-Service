// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models;

namespace InstanceService.Api.Utilities.Interfaces;

/// <summary>
/// Interface for executing graph queries related to instance completeness checks.
/// Replaces the previous ICypherQueryExecutor — same methods, but renamed to be database-agnostic.
/// </summary>
public interface IGraphQueryExecutor
{
    /// <summary>
    /// Executes a completeness query to find all related instances for a given instance ID.
    /// Walks the graph starting from the given instance and returns all connected instances
    /// whose ClassificationId is in the relevant classes list.
    /// </summary>
    Task<IEnumerable<Instance>> ExecuteCompletenessQueryAsync(string instanceId, List<string> relevantClasses);

    /// <summary>
    /// Finds all instances with the given classification IDs.
    /// </summary>
    Task<IEnumerable<Instance>> FindCandidateInstancesAsync(List<string> relevantClasses);
}