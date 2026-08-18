// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstanceService.Models;

namespace InstanceService.Domain.IRepositories;
/// <summary>
/// Represents the interface for accessing access rights.
/// </summary>
public interface IAccessRightRepository
{
    /// <summary>
    /// Retrieves all access rights.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the collection of access rights.</returns>
    public Task<IEnumerable<AccessRight>> GetAccessRights();

    /// <summary>
    /// Retrieves access rights based on user group and use case.
    /// </summary>
    /// <param name="userGroupId">The user group ID.</param>
    /// <param name="useCaseId">The use case ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the collection of access rights.</returns>
    public Task<IEnumerable<AccessRight>> GetAccessRights(string userGroupId, string useCaseId);

    /// <summary>
    /// Retrieves access rights based on user group, use case, and classification.
    /// </summary>
    /// <param name="userGroupId">The user group ID.</param>
    /// <param name="useCaseId">The use case ID.</param>
    /// <param name="classificationId">The classification ID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the collection of access rights.</returns>
    public Task<IEnumerable<AccessRight>> GetAccessRights(string userGroupId, string useCaseId, string classificationId);
}
