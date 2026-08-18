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
/// Provides extension methods for adding client services to the application.
/// These services are specific to the client-side of your application, such as setting up Blazor Server-Side, SignalR, or gRPC.
/// </summary>
public static class ClientServicesExtensions
{
    /// <summary>
    /// Adds client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddClientServices(this IServiceCollection services)
    {
        return services;
    }
}