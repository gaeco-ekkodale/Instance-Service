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

namespace InstanceService.Api.Utilities.Provider;

/// <summary>
/// Interface for fetching access rights from the Access Service API
/// </summary>
public interface IAccessRightsFetcher
{
    /// <summary>
    /// Gets all access rights
    /// </summary>
    /// <returns>A collection of access rights</returns>
    Task<IEnumerable<AccessRight>> GetAccessRightsAsync();

    /// <summary>
    /// Gets access rights for a specific user group and use case
    /// </summary>
    /// <param name="userGroupId">The user group identifier</param>
    /// <param name="useCaseId">The use case identifier</param>
    /// <returns>A collection of access rights for the specified user group and use case</returns>
    Task<IEnumerable<AccessRight>?> GetAccessRightsByUseCaseUserGroupAsync(string userGroupId, string useCaseId);
}
