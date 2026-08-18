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
/// Interface for validating the completeness of graph data based on use case requirements
/// </summary>
public interface ICompletenessCheck
{
    /// <summary>
    /// Checks all use cases for completeness for a given instance and sends GraphDataModel if complete
    /// </summary>
    /// <param name="instanceId">The ID of the instance to check</param>
    Task CheckAndSendAsync(string instanceId);

    /// <summary>
    /// Checks all use cases for completeness for multiple instances and sends GraphDataModel if complete.
    /// Automatically handles duplicate subgraphs.
    /// </summary>
    /// <param name="instanceIds">Array of instance IDs to check</param>
    Task CheckAndSendAsync(string[] instanceIds);

    /// <summary>
    /// Checks if a graph is complete for a specific use case based on an instance ID
    /// </summary>
    /// <param name="instanceId">The ID of the instance to check</param>
    /// <param name="useCaseId">The ID of the use case</param>
    /// <returns>True if the graph is complete for the use case, otherwise false</returns>
    Task<bool> IsUseCaseCompleteAsync(string instanceId, string useCaseId);

    /// <summary>
    /// Finds all complete subgraphs for a specific use case without requiring a start instance.
    /// Each subgraph will be sent as a separate message.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case to check</param>
    /// <returns>List of root instance IDs that form complete subgraphs</returns>
    Task<List<string>> FindAndSendCompleteSubgraphsAsync(string useCaseId);

    /// <summary>
    /// Gets all use case IDs from the classification map
    /// </summary>
    /// <returns>Collection of use case IDs</returns>
    Task<IEnumerable<string>> GetAllUseCaseIdsAsync();
}
