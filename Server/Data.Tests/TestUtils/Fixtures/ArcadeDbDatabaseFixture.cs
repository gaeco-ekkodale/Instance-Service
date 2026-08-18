// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure.IO.GraphBinary;

namespace InstanceService.Data.Tests.TestUtils.Fixtures;

public class ArcadeDbDatabaseFixture : IDisposable
{
    private bool _disposed;
    private readonly string _containerId;
    private readonly GraphTraversalSource _g;
    private readonly IGremlinClient _gremlinClient;

    /// <summary>
    /// Returns the GraphTraversalSource after clearing all graph data for test isolation.
    /// Each access drops all vertices and edges before returning.
    /// </summary>
    public GraphTraversalSource TraversalSource
    {
        get
        {
            _g.V().Drop().Promise(t => t.Iterate()).Wait();
            return _g;
        }
    }

    public ArcadeDbDatabaseFixture()
    {
        _containerId = ArcadeDbContainerHandler.StartDockerAndGetDockerIdAsync().Result;

        var (hostname, port, user, password) =
            ArcadeDbContainerHandler.GetGremlinConnectionParameter(_containerId).Result;

        var server = new GremlinServer(hostname, port, false, user, password);
        _gremlinClient = new GremlinClient(server, new GraphBinaryMessageSerializer());

        var remote = new DriverRemoteConnection(_gremlinClient, "g");
        _g = AnonymousTraversalSource.Traversal().With(remote);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                _gremlinClient.Dispose();

            ArcadeDbContainerHandler.EnsureDockerContainersStoppedAndRemovedAsync(_containerId).Wait();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
