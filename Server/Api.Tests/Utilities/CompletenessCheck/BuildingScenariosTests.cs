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

namespace InstanceService.Api.Tests.Utilities.CompletenessCheck;

/// <summary>
/// Tests for all BuildingScenarios test data
/// Tests standard building-related scenarios with AccessRight variants
/// </summary>
public class BuildingScenariosTests
{
    #region Scenario 1: CompleteSimpleGraph

    [Theory]
    [InlineData(BuildingScenarios.AccessRightVariant.AllRead)]
    [InlineData(BuildingScenarios.AccessRightVariant.AllNone)]
    [InlineData(BuildingScenarios.AccessRightVariant.Mixed)]
    public async Task CompleteSimpleGraph_WithAllVariants_ShouldBeComplete(
        BuildingScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"CompleteSimpleGraph should be complete with {variant} variant");

        // Verify all required classifications are present
        instances.Should().ContainSingle(i => i.ClassificationId == BuildingScenarios.BuildingClassId);
        instances.Should().ContainSingle(i => i.ClassificationId == BuildingScenarios.FloorClassId);
        instances.Should().ContainSingle(i => i.ClassificationId == BuildingScenarios.RoomClassId);
    }

    #endregion

    #region Scenario 2: IncompleteGraphMissingProperty

    [Fact]
    public async Task IncompleteGraphMissingProperty_WithAllRead_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingProperty(
            BuildingScenarios.AccessRightVariant.AllRead);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Floor is missing required Height property");
    }

    [Theory]
    [InlineData(BuildingScenarios.AccessRightVariant.AllNone)]
    [InlineData(BuildingScenarios.AccessRightVariant.Mixed)]
    public async Task IncompleteGraphMissingProperty_WithNoneOrMixed_ShouldBeComplete(
        BuildingScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingProperty(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"Missing property should be ignored with {variant} variant");
    }

    #endregion

    #region Scenario 3: IncompleteGraphMissingClass

    [Theory]
    [InlineData(BuildingScenarios.AccessRightVariant.AllRead)]
    [InlineData(BuildingScenarios.AccessRightVariant.AllNone)]
    [InlineData(BuildingScenarios.AccessRightVariant.Mixed)]
    public async Task IncompleteGraphMissingClass_WithAnyVariant_ShouldBeIncomplete(
        BuildingScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingClass(variant);
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Floor classification is missing, graph is always incomplete");
        instances.Should().NotContain(i => i.ClassificationId == BuildingScenarios.FloorClassId);
    }

    #endregion

    #region Scenario 4: CompleteComplexGraph

    [Fact]
    public async Task CompleteComplexGraph_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteComplexGraph();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("CompleteComplexGraph has all required data");
        instances.Should().HaveCount(6, "graph should have 1 building, 2 floors, 3 rooms");
    }

    #endregion

    #region Scenario 5: MultipleCompleteSubgraphs

    [Fact]
    public async Task MultipleCompleteSubgraphs_BothBuildingsShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.MultipleCompleteSubgraphs();

        // Get both building IDs
        var buildingIds = instances
            .Where(i => i.ClassificationId == BuildingScenarios.BuildingClassId)
            .Select(i => i.Id)
            .ToList();

        buildingIds.Should().HaveCount(2, "there should be two separate buildings");

        // Act & Assert - check both buildings
        foreach (var buildingId in buildingIds)
        {
            var completenessCheck = SetupMocks(instances, accessRights, useCaseId);
            var result = await completenessCheck.IsUseCaseCompleteAsync(buildingId, useCaseId);
            result.Should().BeTrue($"building {buildingId} subgraph should be complete");
        }
    }

    #endregion

    #region Scenario 6: MultipleUseCasesOverlappingClasses

    [Fact]
    public async Task MultipleUseCasesOverlappingClasses_ArchitecturalUseCase_ShouldBeComplete()
    {
        // Arrange
        var (instances, architecturalRights, structuralRights) = BuildingScenarios.MultipleUseCasesOverlappingClasses();
        var completenessCheck = SetupMocks(instances, architecturalRights, BuildingScenarios.ArchitecturalUseCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(
            instances[0].Id,
            BuildingScenarios.ArchitecturalUseCaseId);

        // Assert
        result.Should().BeTrue("architectural use case should be complete");
    }

    [Fact]
    public async Task MultipleUseCasesOverlappingClasses_StructuralUseCase_ShouldBeComplete()
    {
        // Arrange
        var (instances, architecturalRights, structuralRights) = BuildingScenarios.MultipleUseCasesOverlappingClasses();
        var completenessCheck = SetupMocks(instances, structuralRights, BuildingScenarios.StructuralUseCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(
            instances[0].Id,
            BuildingScenarios.StructuralUseCaseId);

        // Assert
        result.Should().BeTrue("structural use case should be complete");
    }

    #endregion

    #region Scenario 7: IncompleteGraphEmptyProperty

    [Fact]
    public async Task IncompleteGraphEmptyProperty_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphEmptyProperty();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("empty string properties should be treated as incomplete");

        var building = instances.First(i => i.ClassificationId == BuildingScenarios.BuildingClassId);
        building.Properties[BuildingScenarios.DescriptionProperty].Should().BeEmpty();
    }

    #endregion

    #region Scenario 8: NoAccessRightsForUseCase

    [Fact]
    public async Task NoAccessRightsForUseCase_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.NoAccessRightsForUseCase();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("no access rights means incomplete graph");
        accessRights.Should().BeEmpty();
    }

    #endregion

    #region Scenario 9: ComplexGraphWithOpenings

    [Fact]
    public async Task ComplexGraphWithOpenings_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.ComplexGraphWithOpenings();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("complex graph with doors and windows should be complete");
        instances.Should().HaveCount(6, "should have Building, Floor, Room, Wall, Door, Window");
        instances.Should().ContainSingle(i => i.ClassificationId == BuildingScenarios.DoorClassId);
        instances.Should().ContainSingle(i => i.ClassificationId == BuildingScenarios.WindowClassId);
    }

    #endregion

    #region Scenario 10: CompleteGraphWithCircularReferences

    [Fact]
    public async Task CompleteGraphWithCircularReferences_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteGraphWithCircularReferences();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("circular references should not prevent completeness");

        // Verify circular relationships exist
        var building = instances.First(i => i.ClassificationId == BuildingScenarios.BuildingClassId);
        var floor = instances.First(i => i.ClassificationId == BuildingScenarios.FloorClassId);

        building.Relations.Should().Contain(r => r.ObjectId == floor.Id);
        floor.Relations.Should().Contain(r => r.ObjectId == building.Id);
    }

    #endregion

    #region Scenario 11: SingleInstanceComplete

    [Fact]
    public async Task SingleInstanceComplete_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.SingleInstanceComplete();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("single instance with all properties should be complete");
        instances.Should().ContainSingle();
    }

    #endregion

    #region Scenario 12: MixedCompleteAndIncompleteSubgraphs

    [Fact]
    public async Task MixedCompleteAndIncompleteSubgraphs_CompleteBuilding_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.MixedCompleteAndIncompleteSubgraphs();

        // Find the complete building (building-12-1)
        var completeBuilding = instances.First(i => i.Id == "building-12-1");

        // Setup mocks with only the complete subgraph
        var completeSubgraph = instances.Where(i =>
            i.Id.Contains("12-1")).ToList();
        var completenessCheck = SetupMocks(completeSubgraph, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(completeBuilding.Id, useCaseId);

        // Assert
        result.Should().BeTrue("complete building subgraph should be complete");
    }

    [Fact]
    public async Task MixedCompleteAndIncompleteSubgraphs_IncompleteBuilding_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.MixedCompleteAndIncompleteSubgraphs();

        // Find the incomplete building (building-12-2)
        var incompleteBuilding = instances.First(i => i.Id == "building-12-2");

        // Setup mocks with only the incomplete subgraph
        var incompleteSubgraph = instances.Where(i =>
            i.Id.Contains("12-2")).ToList();
        var completenessCheck = SetupMocks(incompleteSubgraph, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(incompleteBuilding.Id, useCaseId);

        // Assert
        result.Should().BeFalse("incomplete building subgraph should be incomplete (missing room area)");
    }

    #endregion

    #region Scenario 13: GraphWithIrrelevantInstances

    [Fact]
    public async Task GraphWithIrrelevantInstances_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.GraphWithIrrelevantInstances();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("irrelevant instances (Door) should not affect completeness");

        // Verify Door instance exists but is not in access rights
        instances.Should().Contain(i => i.ClassificationId == BuildingScenarios.DoorClassId);
        accessRights.Should().NotContain(ar => ar.GuidelineClassificationId == BuildingScenarios.DoorClassId);
    }

    #endregion

    #region Scenario 14: EmptyGraph

    [Fact]
    public async Task EmptyGraph_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.EmptyGraph();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync("any-id", useCaseId);

        // Assert
        result.Should().BeFalse("empty graph cannot be complete");
        instances.Should().BeEmpty();
    }

    #endregion

    #region Scenario 15: NonExistentUseCaseId

    [Fact]
    public async Task NonExistentUseCaseId_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.NonExistentUseCaseId();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("non-existent use case ID should result in incomplete");
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
