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
using InstanceService.Api.Tests.Utilities.CompletenessCheck.TestData;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using InstanceService.Models.Enum;
using System.Diagnostics;

namespace InstanceService.Api.Tests.Utilities.CompletenessCheck;

/// <summary>
/// Tests for all PerformanceScenarios test data
/// Tests large-scale scenarios with different AccessRight variants
/// </summary>
public class PerformanceScenariosTests
{
    #region Performance 1: LargeCompleteGraph

    [Theory]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllRead)]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllNone)]
    [InlineData(PerformanceScenarios.AccessRightVariant.Mixed)]
    public async Task LargeCompleteGraph_WithAllVariants_ShouldBeComplete(
        PerformanceScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.LargeCompleteGraph(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue($"large complete graph (211 instances) should be complete with {variant} variant");
        instances.Should().HaveCount(211, "1 building + 10 floors + 200 rooms");

        // Performance assertion - should complete quickly even with 211 instances
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "should complete within 1 second");
    }

    #endregion

    #region Performance 2: MultipleLargeBuildings

    [Theory]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllRead)]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllNone)]
    [InlineData(PerformanceScenarios.AccessRightVariant.Mixed)]
    public async Task MultipleLargeBuildings_WithAllVariants_ShouldBeComplete(
        PerformanceScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.MultipleLargeBuildings(variant);

        // Get all building IDs
        var buildingIds = instances
            .Where(i => i.ClassificationId == PerformanceScenarios.BuildingClassId)
            .Select(i => i.Id)
            .ToList();

        // Act - test each building with fresh instance
        var results = new List<bool>();
        foreach (var buildingId in buildingIds)
        {
            var completenessCheck = SetupMocks(instances, accessRights, useCaseId);
            var result = await completenessCheck.IsUseCaseCompleteAsync(buildingId, useCaseId);
            results.Add(result);
        }

        // Assert
        results.Should().OnlyContain(r => r == true, "all 5 buildings should be complete");
    }

    #endregion

    #region Performance 3: HighlyInterconnectedGraph

    [Theory]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllRead)]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllNone)]
    [InlineData(PerformanceScenarios.AccessRightVariant.Mixed)]
    public async Task HighlyInterconnectedGraph_WithAllVariants_ShouldBeComplete(
        PerformanceScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.HighlyInterconnectedGraph(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"highly interconnected mesh graph should be complete with {variant} variant");
        instances.Should().HaveCount(20, "20 nodes in mesh topology");

        // Verify high interconnectivity
        var totalRelations = instances.Sum(i => i.Relations.Count);
        totalRelations.Should().Be(100, "20 nodes × 5 connections each = 100 relations");
    }

    #endregion

    #region Performance 4: ManyPropertiesPerInstance

    [Theory]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllRead)]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllNone)]
    [InlineData(PerformanceScenarios.AccessRightVariant.Mixed)]
    public async Task ManyPropertiesPerInstance_WithAllVariants_ShouldBeComplete(
        PerformanceScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.ManyPropertiesPerInstance(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"instances with 50 properties each should be complete with {variant} variant");

        // Verify property counts
        var building = instances.First(i => i.ClassificationId == PerformanceScenarios.BuildingClassId);
        var floor = instances.First(i => i.ClassificationId == PerformanceScenarios.FloorClassId);

        building.Properties.Should().HaveCount(50);
        floor.Properties.Should().HaveCount(50);
        accessRights.Should().HaveCount(100, "50 properties × 2 instances = 100 access rights");
    }

    #endregion

    #region Performance 5: ManyUseCasesSameGraph

    [Theory]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllRead)]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllNone)]
    [InlineData(PerformanceScenarios.AccessRightVariant.Mixed)]
    public async Task ManyUseCasesSameGraph_WithAllVariants_AllUseCasesShouldBeComplete(
        PerformanceScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRightsByUseCase) = PerformanceScenarios.ManyUseCasesSameGraph(variant);

        accessRightsByUseCase.Should().HaveCount(10, "there should be 10 use cases");

        // Act & Assert - check each use case with fresh instance
        foreach (var useCaseEntry in accessRightsByUseCase)
        {
            var useCaseId = useCaseEntry.Key;
            var accessRights = useCaseEntry.Value;

            var completenessCheck = SetupMocks(instances, accessRights, useCaseId);
            var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

            result.Should().BeTrue($"use case {useCaseId} should be complete with {variant} variant");
        }
    }

    #endregion

    #region Performance 6: ComplexRealisticBuilding

    [Theory]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllRead)]
    [InlineData(PerformanceScenarios.AccessRightVariant.AllNone)]
    [InlineData(PerformanceScenarios.AccessRightVariant.Mixed)]
    public async Task ComplexRealisticBuilding_WithAllVariants_ShouldBeComplete(
        PerformanceScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.ComplexRealisticBuilding(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"complex realistic building should be complete with {variant} variant");

        // Verify structure: Building → Floor → Room → Wall → Door/Window
        instances.Should().Contain(i => i.ClassificationId == PerformanceScenarios.BuildingClassId);
        instances.Should().Contain(i => i.ClassificationId == PerformanceScenarios.FloorClassId);
        instances.Should().Contain(i => i.ClassificationId == PerformanceScenarios.RoomClassId);
        instances.Should().Contain(i => i.ClassificationId == PerformanceScenarios.WallClassId);
        instances.Should().Contain(i => i.ClassificationId == PerformanceScenarios.DoorClassId);
        instances.Should().Contain(i => i.ClassificationId == PerformanceScenarios.WindowClassId);

        // Calculate expected count: 1 building + 3 floors + (3×5=15 rooms) + (15×4=60 walls) + (60×2=120 openings)
        // Total: 1 + 3 + 15 + 60 + 120 = 199 instances
        instances.Should().HaveCount(199, "complex building has all structural elements");
    }

    #endregion

    #region Performance Verification Tests

    [Fact]
    public async Task LargeCompleteGraph_PerformanceCheck_ShouldCompleteQuickly()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.LargeCompleteGraph();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Warm up
        await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Create new instance for actual measurement
        completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act - measure actual performance
        var stopwatch = Stopwatch.StartNew();
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "completeness check for 211 instances should be very fast (< 100ms)");
    }

    [Fact]
    public async Task ManyPropertiesPerInstance_PropertyCheckPerformance_ShouldBeEfficient()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.ManyPropertiesPerInstance(
            PerformanceScenarios.AccessRightVariant.AllRead);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        var stopwatch = Stopwatch.StartNew();

        // Act - checking 50 properties per instance
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
            "checking 100 properties should be efficient (< 200ms)");
    }

    #endregion

    #region AccessRight Variant Behavior Tests

    [Fact]
    public async Task LargeCompleteGraph_AllRead_RequiresAllProperties()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.LargeCompleteGraph(
            PerformanceScenarios.AccessRightVariant.AllRead);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("all Read properties are present");
        accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.Read);
    }

    [Fact]
    public async Task LargeCompleteGraph_AllNone_IgnoresProperties()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.LargeCompleteGraph(
            PerformanceScenarios.AccessRightVariant.AllNone);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("properties are ignored with None variant");
        accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.None);
    }

    [Fact]
    public async Task LargeCompleteGraph_Mixed_RequiresOnlyReadProperties()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = PerformanceScenarios.LargeCompleteGraph(
            PerformanceScenarios.AccessRightVariant.Mixed);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("only Read properties are required");
        accessRights.Should().Contain(ar => ar.Right == PropertyRight.Read);
        accessRights.Should().Contain(ar => ar.Right == PropertyRight.None);
    }

    #endregion

    #region Helper Methods

    private Api.Utilities.CompletenessCheck SetupMocks(List<Instance> instances, List<AccessRight> accessRights, string useCaseId)
    {
        // Create fresh mocks for each test to avoid cache issues
        var cypherQueryExecutor = Substitute.For<IGraphQueryExecutor>();
        var accessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        var instanceRepository = Substitute.For<IInstanceRepository>();
        var logger = Substitute.For<ILogger<Api.Utilities.CompletenessCheck>>();
        var dynamicKafkaProducer = Substitute.For<IDynamicKafkaProducer>();
        var guidelineProvider = Substitute.For<IGuidelineProvider>();

        // Setup mocks
        accessRightsFetcher.GetAccessRightsAsync()
            .Returns(Task.FromResult<IEnumerable<AccessRight>>(accessRights));

        foreach (var instance in instances)
        {
            instanceRepository.GetInstance(instance.Id)
                .Returns(Task.FromResult<Instance?>(instance));
        }

        cypherQueryExecutor.ExecuteCompletenessQueryAsync(
                Arg.Any<string>(),
                Arg.Any<List<string>>())
            .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

        cypherQueryExecutor.FindCandidateInstancesAsync(Arg.Any<List<string>>())
            .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

        // Create and return new CompletenessCheck instance
        return new Api.Utilities.CompletenessCheck(
            cypherQueryExecutor,
            accessRightsFetcher,
            instanceRepository,
            logger,
            dynamicKafkaProducer,
            guidelineProvider
        );
    }

    #endregion
}
