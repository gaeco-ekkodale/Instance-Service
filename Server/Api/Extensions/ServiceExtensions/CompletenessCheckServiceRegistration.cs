// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;

namespace InstanceService.Api.Extensions.ServiceExtensions;

/// <summary>
/// Service registration extensions for the CompletenessCheck service
/// </summary>
public static class CompletenessCheckServiceRegistrationExample
{
    /// <summary>
    /// Registers the CompletenessCheck service with the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCompletenessCheck(this IServiceCollection services)
    {
        // Register as singleton to benefit from internal caching
        services.AddSingleton<ICompletenessCheck, CompletenessCheck>();
        
        return services;
    }

    /// <summary>
    /// Alternative registration for scoped lifetime
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCompletenessCheckScoped(this IServiceCollection services)
    {
        // Register as scoped if you need fresh instances per request
        services.AddScoped<ICompletenessCheck, CompletenessCheck>();
        
        return services;
    }
}
