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

namespace InstanceService.Api.Utilities.Provider
{
    /// <summary>
    /// Interface for fetching usecases from the UseCase Service API
    /// </summary>
    public interface IUseCaseFetcher
    {
        /// <summary>
        /// Gets usecase by usecaseId
        /// </summary>
        /// <param name="useCaseId">The usecase identifier</param>
        /// <returns>The usecase with the specified id</returns>
        Task<UseCase> GetUseCasesByIdAsync(string useCaseId);
    }
}
