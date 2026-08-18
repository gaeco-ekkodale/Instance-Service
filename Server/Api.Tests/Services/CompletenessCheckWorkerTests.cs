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
using InstanceService.Api.Services;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InstanceService.Api.Tests.Services;

/// <summary>
/// Tests that queued completeness checks are executed in the background, batched and
/// isolated from each other.
/// </summary>
public class CompletenessCheckWorkerTests
{
    /// <summary>Generous upper bound; the worker debounces a batch for ~500 ms.</summary>
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private readonly ICompletenessCheck _completenessCheck = Substitute.For<ICompletenessCheck>();
    private readonly CompletenessCheckScheduler _scheduler = new();

    private CompletenessCheckWorker CreateWorker()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_completenessCheck);

        return new CompletenessCheckWorker(
            _scheduler,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<CompletenessCheckWorker>>());
    }

    /// <summary>
    /// Captures the instance ids of the next batch the worker hands to the completeness check.
    /// </summary>
    private Task<string[]> CaptureNextBatch()
    {
        var batch = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        _completenessCheck.CheckAndSendAsync(Arg.Any<string[]>()).Returns(call =>
        {
            batch.TrySetResult(call.Arg<string[]>());
            return Task.CompletedTask;
        });

        return batch.Task;
    }

    [Fact]
    public async Task Schedule_CollapsesBurstIntoASingleDeduplicatedBatch()
    {
        var worker = CreateWorker();
        var batch = CaptureNextBatch();

        await worker.StartAsync(CancellationToken.None);

        _scheduler.Schedule("instance-1");
        _scheduler.Schedule(["instance-2", "instance-1"]);

        var instanceIds = await batch.WaitAsync(WaitTimeout);

        instanceIds.Should().BeEquivalentTo(["instance-1", "instance-2"],
            "a burst of changes must result in one check per distinct instance");

        await worker.StopAsync(CancellationToken.None);
        await _completenessCheck.Received(1).CheckAndSendAsync(Arg.Any<string[]>());
    }

    [Fact]
    public async Task Schedule_IgnoresEmptyInstanceIds()
    {
        var worker = CreateWorker();
        var batch = CaptureNextBatch();

        await worker.StartAsync(CancellationToken.None);

        _scheduler.Schedule((string?)null);
        _scheduler.Schedule(string.Empty);
        _scheduler.Schedule("instance-1");

        var instanceIds = await batch.WaitAsync(WaitTimeout);

        instanceIds.Should().BeEquivalentTo(["instance-1"]);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FailingCheck_DoesNotStopTheWorker()
    {
        var worker = CreateWorker();

        var firstBatch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondBatch = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        _completenessCheck.CheckAndSendAsync(Arg.Any<string[]>()).Returns(call =>
        {
            if (firstBatch.TrySetResult())
                throw new InvalidOperationException("no related instances found");

            secondBatch.TrySetResult(call.Arg<string[]>());
            return Task.CompletedTask;
        });

        await worker.StartAsync(CancellationToken.None);

        _scheduler.Schedule("broken-instance");
        await firstBatch.Task.WaitAsync(WaitTimeout);

        _scheduler.Schedule("healthy-instance");
        var instanceIds = await secondBatch.Task.WaitAsync(WaitTimeout);

        instanceIds.Should().BeEquivalentTo(["healthy-instance"],
            "a failing check must not take the worker down");

        await worker.StopAsync(CancellationToken.None);
    }
}
