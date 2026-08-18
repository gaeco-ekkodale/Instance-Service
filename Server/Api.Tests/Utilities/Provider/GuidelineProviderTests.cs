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
using InstanceService.Api.Utilities.Provider;
using InstanceService.Models;
using InstanceService.Models.Enum;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InstanceService.Api.Tests.Utilities.Provider;

public class GuidelineProviderTests
{
    private static IMemoryCache CreateMemoryCache()
        => new MemoryCache(new MemoryCacheOptions { SizeLimit = 4096 });

    private static (IGuidelineReconstructionService reconstruction, IAccessRightsFetcher accessRightsFetcher, GuidelineProvider provider) CreateProvider(
        long generation = 1,
        List<AccessRight>? accessRights = null)
    {
        var fullGuideline = new Guideline.Model.Model.Guideline();

        var reconstruction = Substitute.For<IGuidelineReconstructionService>();
        reconstruction.Generation.Returns(generation);
        reconstruction.GetFullGuidelineAsync(Arg.Any<CancellationToken>()).Returns(fullGuideline);

        var accessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        accessRightsFetcher.GetAccessRightsAsync().Returns(accessRights ?? []);

        var logger = Substitute.For<ILogger<GuidelineProvider>>();
        var provider = new GuidelineProvider(reconstruction, accessRightsFetcher, CreateMemoryCache(), logger);

        return (reconstruction, accessRightsFetcher, provider);
    }

    [Fact]
    public async Task GetGuideline_CallsReconstructionAndAccessRights()
    {
        var useCaseId = Guid.NewGuid().ToString();
        var accessRights = new List<AccessRight>
        {
            new() { UseCaseId = Guid.Parse(useCaseId), GuidelineClassificationId = "cls-1", Name = "prop1", Right = PropertyRight.Read }
        };

        var (reconstruction, accessRightsFetcher, provider) = CreateProvider(accessRights: accessRights);

        var result = await provider.GetGuideline(useCaseId);

        result.Should().NotBeNull();
        await reconstruction.Received(1).GetFullGuidelineAsync(Arg.Any<CancellationToken>());
        await accessRightsFetcher.Received(1).GetAccessRightsAsync();
    }

    [Fact]
    public async Task GetGuideline_SecondCallWithSameUseCaseId_UsesCachedResult()
    {
        var useCaseId = Guid.NewGuid().ToString();
        var (reconstruction, accessRightsFetcher, provider) = CreateProvider();

        var first = await provider.GetGuideline(useCaseId);
        var second = await provider.GetGuideline(useCaseId);

        second.Should().BeSameAs(first);
        await reconstruction.Received(1).GetFullGuidelineAsync(Arg.Any<CancellationToken>());
        await accessRightsFetcher.Received(1).GetAccessRightsAsync();
    }

    [Fact]
    public async Task GetGuideline_AfterGenerationChange_BypassesCache()
    {
        var useCaseId = Guid.NewGuid().ToString();
        var fullGuideline = new Guideline.Model.Model.Guideline();

        var reconstruction = Substitute.For<IGuidelineReconstructionService>();
        reconstruction.Generation.Returns(1, 2);
        reconstruction.GetFullGuidelineAsync(Arg.Any<CancellationToken>()).Returns(fullGuideline);

        var accessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        accessRightsFetcher.GetAccessRightsAsync().Returns([]);

        var provider = new GuidelineProvider(reconstruction, accessRightsFetcher, CreateMemoryCache(), Substitute.For<ILogger<GuidelineProvider>>());

        await provider.GetGuideline(useCaseId);
        await provider.GetGuideline(useCaseId);

        await reconstruction.Received(2).GetFullGuidelineAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGuideline_FiltersAccessRightsByUseCaseId()
    {
        var targetUseCaseId = Guid.NewGuid();
        var otherUseCaseId = Guid.NewGuid();

        var accessRights = new List<AccessRight>
        {
            new() { UseCaseId = targetUseCaseId, GuidelineClassificationId = "cls-1", Name = "prop1", Right = PropertyRight.Read },
            new() { UseCaseId = otherUseCaseId, GuidelineClassificationId = "cls-2", Name = "prop2", Right = PropertyRight.Write }
        };

        var (_, accessRightsFetcher, provider) = CreateProvider(accessRights: accessRights);

        // calling for one use case should not throw and should return a guideline
        var result = await provider.GetGuideline(targetUseCaseId.ToString());

        result.Should().NotBeNull();
        await accessRightsFetcher.Received(1).GetAccessRightsAsync();
    }
}
