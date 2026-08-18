// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Data;
using InstanceService.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure.IO.GraphBinary;

namespace InstanceService.Api.Extensions.ServiceExtensions;

/// <summary>
/// Extensions for data access related operations such as Entity Framework Core, Dapper, or other ORMs.
/// Configure your DbContexts, repository interfaces, and their implementations here.
/// </summary>
public static class DataAccessExtensions
{
    /// <summary>
    /// Adds data access related services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the data access services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddGremlin();
        services.AddPostgres();

        return services;
    }

    /// <summary>
    /// Registers the Gremlin client and the GraphTraversalSource in the DI container.
    ///
    /// What happens here?
    /// 1. GremlinServer = the connection details (hostname, port, user, password)
    /// 2. GremlinClient = the actual client that communicates with the server
    /// 3. GraphTraversalSource = the "g" object through which all queries run
    /// (if you see g.V().Has(...) in the repositories — the "g" comes from here)
    ///
    /// Both are registered as singletons, i.e. there is only one connection
    /// that is reused for the entire lifetime of the app.
    ///
    /// Comparison with Neo4j:
    /// - Neo4j: IGraphClient → graphClient.Cypher.Match(...)
    /// - Gremlin: GraphTraversalSource → g.V().Has(...)
    /// </summary>
    private static void AddGremlin(this IServiceCollection services)
    {
        services.AddSingleton<IGremlinClient>(provider =>
        {
            var gremlinOptions = provider.GetRequiredService<IOptions<GremlinOptions>>().Value;

            // ArcadeDB expects the username as a simple string (e.g. "root"),
            // NOT in the TinkerPop path format "/{database}/{user}".
            var gremlinServer = new GremlinServer(
                hostname: gremlinOptions.Hostname,
                port: gremlinOptions.Port,
                enableSsl: gremlinOptions.EnableSSL,
                username: gremlinOptions.User,
                password: gremlinOptions.Password);

            return new GremlinClient(
                gremlinServer,
                new GraphBinaryMessageSerializer());
        });

        // "g" is the default name for the TraversalSource in TinkerPop.
        // All Gremlin queries start with g.V() (vertices) or g.E() (edges).
        services.AddSingleton<GraphTraversalSource>(provider =>
        {
            var client = provider.GetRequiredService<IGremlinClient>();
            var remote = new DriverRemoteConnection(client, "g");
            return AnonymousTraversalSource.Traversal().With(remote);
        });
    }

    private static void AddPostgres(this IServiceCollection services)
    {
        services.AddDbContext<InstanceServiceDbContext>((provider, builder) =>
        {
            var postgresOptions = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;

            builder.UseNpgsql(
                $"Host={postgresOptions.Host};" +
                $"Port={postgresOptions.Port};" +
                $"Database={postgresOptions.Database};" +
                $"Username={postgresOptions.User};" +
                $"Password={postgresOptions.Password}");
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }
}