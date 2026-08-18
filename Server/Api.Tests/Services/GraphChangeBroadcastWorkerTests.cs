// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentAssertions;
using InstanceService.Api.Hubs;
using InstanceService.Api.Services;
using InstanceService.Api.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace InstanceService.Api.Tests.Services;

/// <summary>
/// A window of changes reaches the clients of a use case as one announcement, and a use case
/// written to repeatedly is announced once rather than once per write.
/// </summary>
public class GraphChangeBroadcastWorkerTests : IDisposable
{
    private const string UseCaseId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    private const string OtherUseCaseId = "8a1b0c4d-2e3f-4a5b-9c8d-7e6f5a4b3c2d";

    /// <summary>
    /// How long a test waits for an announcement. Generous, because a healthy worker signals
    /// within one collection window and only a broken one ever pays this in full.
    /// </summary>
    private static readonly TimeSpan AnnouncementTimeout =
        GraphChangeBroadcastWorker.BroadcastWindow + TimeSpan.FromSeconds(10);

    private readonly IHubClients _clients = Substitute.For<IHubClients>();
    private readonly IClientProxy _proxy = Substitute.For<IClientProxy>();
    private readonly GraphChangeNotifier _notifier = new();

    /// <summary>Released once per announcement the worker sends.</summary>
    private readonly SemaphoreSlim _announcements = new(0);

    private GraphChangeBroadcastWorker CreateWorker()
    {
        var hub = Substitute.For<IHubContext<GraphHub>>();
        hub.Clients.Returns(_clients);
        _clients.Group(Arg.Any<string>()).Returns(_proxy);

        _proxy
            .When(proxy => proxy.SendCoreAsync(
                Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>()))
            .Do(_ => _announcements.Release());

        return new GraphChangeBroadcastWorker(
            _notifier, hub, Substitute.For<ILogger<GraphChangeBroadcastWorker>>());
    }

    /// <summary>
    /// Runs the worker until it has sent the expected number of announcements, then stops it.
    /// </summary>
    /// <remarks>
    /// Waits for the announcements rather than for the collection window to pass: stopping the
    /// worker cancels the window it is sitting in and discards everything collected in it, so a
    /// fixed wait that ends a moment early - which a loaded build agent is enough to cause -
    /// sees no announcement at all.
    /// <para>
    /// Everything a test queues has to be in the channel before this is called, so that it all
    /// lands in one window.
    /// </para>
    /// </remarks>
    /// <param name="worker">The worker to run.</param>
    /// <param name="expected">The number of announcements to wait for.</param>
    private async Task RunUntilAnnouncedAsync(GraphChangeBroadcastWorker worker, int expected)
    {
        await worker.StartAsync(CancellationToken.None);

        try
        {
            for (var i = 0; i < expected; i++)
            {
                var signalled = await _announcements.WaitAsync(AnnouncementTimeout);

                signalled.Should().BeTrue(
                    "the worker should have sent announcement {0} of {1}", i + 1, expected);
            }
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task AChange_IsAnnouncedToTheGroupOfItsUseCase()
    {
        var worker = CreateWorker();
        _notifier.NotifyChanged(UseCaseId);

        await RunUntilAnnouncedAsync(worker, 1);

        _clients.Received(1).Group(GraphHub.GroupFor(UseCaseId));
        await _proxy.Received(1).SendCoreAsync(
            GraphHub.GraphChangedEvent,
            Arg.Is<object?[]>(args => args.Length == 1 && Equals(args[0], UseCaseId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepeatedChangesOfOneUseCase_AreAnnouncedOnce()
    {
        var worker = CreateWorker();

        _notifier.NotifyChanged(UseCaseId);
        _notifier.NotifyChanged(UseCaseId);
        _notifier.NotifyChanged(UseCaseId);

        await RunUntilAnnouncedAsync(worker, 1);

        // The three writes shared a window, and a window holds a use case once - so there is
        // nothing left in the queue that a second announcement could come from.
        _clients.Received(1).Group(GraphHub.GroupFor(UseCaseId));
    }

    [Fact]
    public async Task ChangesOfSeveralUseCases_AreAnnouncedToEachGroup()
    {
        var worker = CreateWorker();

        _notifier.NotifyChanged(UseCaseId);
        _notifier.NotifyChanged(OtherUseCaseId);

        await RunUntilAnnouncedAsync(worker, 2);

        _clients.Received(1).Group(GraphHub.GroupFor(UseCaseId));
        _clients.Received(1).Group(GraphHub.GroupFor(OtherUseCaseId));
    }

    [Fact]
    public void NotifyChanged_WithoutUseCase_QueuesNothing()
    {
        _notifier.NotifyChanged(null);
        _notifier.NotifyChanged(string.Empty);

        _notifier.Reader.TryRead(out _).Should().BeFalse();
    }

    public void Dispose() => _announcements.Dispose();
}
