// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Docker.DotNet;
using Docker.DotNet.Models;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure.IO.GraphBinary;

namespace InstanceService.Data.Tests.TestUtils;

public static class ArcadeDbContainerHandler
{
    private const string DbPassword = "password";
    private const string DbUsername = "root";
    private const string DbName = "testdb";
    private const string DbImage = "arcadedata/arcadedb";
    private const string DbImageTag = "26.3.1";

    /// <summary>
    /// Starts a new docker container with ArcadeDB (Gremlin enabled on port 8182).
    /// </summary>
    /// <returns>Container id for the ArcadeDB instance.</returns>
    public static async Task<string> StartDockerAndGetDockerIdAsync()
    {
        var dockerClient = GetDockerClient();

        await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
        {
            FromImage = $"{DbImage}:{DbImageTag}"
        }, null, new Progress<JSONMessage>());

        var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = "arcadedb-instance-test",
            Image = $"{DbImage}:{DbImageTag}",
            HostConfig = new HostConfig
            {
                PublishAllPorts = true,
            },
            Env =
            [
                $"JAVA_OPTS=-Darcadedb.server.rootPassword={DbPassword} " +
                $"-Darcadedb.server.plugins=GremlinServer:com.arcadedb.server.gremlin.GremlinServerPlugin " +
                $"-Darcadedb.server.defaultDatabases={DbName}[{DbUsername}:{DbPassword}:admin]"
            ]
        });

        await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
        await WaitUntilDatabaseAvailableAsync(container.ID);

        return container.ID;
    }

    /// <summary>
    /// Stops and removes the docker container.
    /// </summary>
    public static async Task EnsureDockerContainersStoppedAndRemovedAsync(string dockerContainerId)
    {
        var dockerClient = GetDockerClient();
        await dockerClient.Containers.StopContainerAsync(dockerContainerId, new ContainerStopParameters());
        await dockerClient.Containers.RemoveContainerAsync(dockerContainerId,
            new ContainerRemoveParameters { RemoveVolumes = true, Force = true });
    }

    /// <summary>
    /// Gets the Gremlin connection parameters from the running container.
    /// </summary>
    public static async Task<(string hostname, int port, string user, string password)> GetGremlinConnectionParameter(
        string containerId)
    {
        var dockerClient = GetDockerClient();
        var containers =
            await dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

        var container = containers.FirstOrDefault(c => c.ID == containerId)
                        ?? throw new Exception($"Docker container with id {containerId} not found.");

        var gremlinPort = container.Ports.First(p => p.PrivatePort == 8182);

        return ("localhost", (int)gremlinPort.PublicPort, DbUsername, DbPassword);
    }

    private static DockerClient GetDockerClient()
    {
        var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock";

        return new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
    }

    private static async Task WaitUntilDatabaseAvailableAsync(string containerId)
    {
        var (hostname, port, user, password) = await GetGremlinConnectionParameter(containerId);
        const int maxWaitTimeSeconds = 60;
        var connectionEstablished = false;

        var start = DateTime.UtcNow;
        while (!connectionEstablished && start.AddSeconds(maxWaitTimeSeconds) > DateTime.UtcNow)
        {
            try
            {
                var server = new GremlinServer(hostname, port, false, user, password);
                using var client = new GremlinClient(server, new GraphBinaryMessageSerializer());
                var remote = new DriverRemoteConnection(client, "g");
                var g = AnonymousTraversalSource.Traversal().With(remote);

                await g.V().Count().Promise(t => t.Next());
                connectionEstablished = true;
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        if (!connectionEstablished)
            throw new Exception(
                $"Connection to ArcadeDB Gremlin server could not be established within {maxWaitTimeSeconds} seconds.");
    }
}
