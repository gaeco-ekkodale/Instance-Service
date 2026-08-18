// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Api.Extensions.ServiceExtensions;

/// <summary>
/// Provides extension methods for adding health checks and monitoring services to the application.
/// This includes the configuration of health checks, telemetry, and monitoring services.
/// This could include things like Application Insights, Prometheus, health checks for databases, external services, and more.
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds health checks and monitoring services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
        return services;
    }
}