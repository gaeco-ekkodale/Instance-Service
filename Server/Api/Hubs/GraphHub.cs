// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.SignalR;

namespace InstanceService.Api.Hubs;

/// <summary>
/// Tells the clients of a use case that its instances changed, so that they refetch them.
/// </summary>
/// <remarks>
/// Notifications carry nothing but the ID of the use case a caller already knows. The graph is
/// filtered per user by access rights, so instance data in the payload would hand a client
/// instances it is not allowed to read - every client fetches the graph itself, over the
/// authenticated API and with its own token.
/// <para>
/// Unauthenticated, like the plugin host hub. A browser cannot put headers on a web socket
/// handshake, so authenticating here would mean carrying a token in the URL, and the only thing
/// that would buy is hiding the fact that a given use case ID changed - from someone who has to
/// know that ID to ask in the first place, and who still cannot read a single instance without a
/// token.
/// </para>
/// </remarks>
public class GraphHub : Hub
{
    /// <summary>Client method invoked with the ID of the use case that changed.</summary>
    public const string GraphChangedEvent = "GraphChanged";

    /// <summary>Hub method a client calls to receive the changes of one use case.</summary>
    public const string SubscribeMethod = "Subscribe";

    private readonly ILogger<GraphHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphHub"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GraphHub(ILogger<GraphHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// The group carrying the notifications of one use case.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case.</param>
    /// <returns>The group name.</returns>
    public static string GroupFor(string useCaseId) => $"usecase:{useCaseId}";

    /// <summary>
    /// Subscribes this connection to the changes of one use case.
    /// </summary>
    /// <remarks>
    /// Clients call this after connecting and again after every reconnect: group membership
    /// belongs to the connection and is gone once it dropped.
    /// </remarks>
    /// <param name="useCaseId">The ID of the use case to follow.</param>
    public async Task Subscribe(string useCaseId)
    {
        // Group names are held in memory for as long as the connection lives, and a use case is
        // identified by a GUID. Anything else is refused rather than turned into a group, so
        // that a caller cannot make the server remember arbitrary strings.
        if (!Guid.TryParse(useCaseId, out _))
        {
            _logger.LogWarning("Connection {ConnectionId} asked for the malformed use case {UseCaseId}",
                Context.ConnectionId, useCaseId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(useCaseId));

        _logger.LogDebug("Connection {ConnectionId} follows use case {UseCaseId}",
            Context.ConnectionId, useCaseId);
    }
}
