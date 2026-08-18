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
/// Provides extension methods for adding infrastructure services to the application.
/// These services are commonly used across the application and are often of an infrastructure nature,
/// such as logging, caching, configuration management, messaging services (e.g., RabbitMQ, Kafka),
/// and external services integrations (e.g., email services, payment providers).
/// </summary>
public static class InfrastructureServicesExtensions
{
    /// <summary>
    /// Adds RabbitMQ messaging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        return services;
    }
}