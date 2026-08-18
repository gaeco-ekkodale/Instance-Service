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
/// Provides extension methods for adding domain services to the application.
/// These services are closely related to the business logic of your application.
/// This includes the configuration of services that implement business rules, validation services, and similar.
/// </summary>
public static class DomainServicesExtensions
{
    /// <summary>
    /// Adds domain services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services;
    }
}