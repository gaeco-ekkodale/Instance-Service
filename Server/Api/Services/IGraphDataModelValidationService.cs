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

namespace InstanceService.Api.Services;

/// <summary>
/// Defines the contract for a service that validates a <see cref="GraphDataModel"/>.
/// </summary>
public interface IGraphDataModelValidationService
{
    /// <summary>
    /// Asynchronously validates the entire graph data model.
    /// </summary>
    /// <param name="model">The <see cref="GraphDataModel"/> to validate.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous validation operation. The task result contains a <see cref="ValidationResult"/> with the outcome of the validation.</returns>
    Task<ValidationResult> ValidateAsync(GraphDataModel model);

    /// <summary>
    /// Asynchronously validates only the relationships within the graph data model.
    /// </summary>
    /// <param name="model">The <see cref="GraphDataModel"/> whose relationships are to be validated.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous validation operation. The task result contains a <see cref="ValidationResult"/> with the outcome of the relationship validation.</returns>
    Task<ValidationResult> ValidateRelationshipsAsync(GraphDataModel model);

    /// <summary>
    /// Asynchronously validates the access rights for the given graph data model.
    /// </summary>
    /// <param name="model">The <see cref="GraphDataModel"/> to validate.</param>
    /// <param name="groupIds">The list of group IDs to be considered for access right validation.</param>
    /// <param name="useCaseId">The ID of the use case to validate access rights for.</param>
    /// <param name="accessRights">The collection of <see cref="AccessRight"/> to be validated.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous validation operation. The task result contains a <see cref="ValidationResult"/> with the outcome of the access rights validation.</returns>
    Task<ValidationResult> ValidateAccessRightsAsync(GraphDataModel model, List<string> groupIds, string useCaseId, IEnumerable<AccessRight> accessRights);
}
