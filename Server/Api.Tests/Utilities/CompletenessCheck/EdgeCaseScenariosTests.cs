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

namespace InstanceService.Api.Tests.Utilities.CompletenessCheck;

/// <summary>
/// Tests for all EdgeCaseScenarios test data
/// Tests boundary conditions, null/empty values, and unusual configurations
/// </summary>
public class EdgeCaseScenariosTests
{
    private readonly IGraphQueryExecutor _cypherQueryExecutor;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IInstanceRepository _instanceRepository;
    private readonly ILogger<Api.Utilities.CompletenessCheck> _logger;
    private readonly IDynamicKafkaProducer _dynamicKafkaProducer;
    private readonly IGuidelineProvider _guidelineProvider;
    private readonly Api.Utilities.CompletenessCheck _completenessCheck;

    public EdgeCaseScenariosTests()
    {
        _cypherQueryExecutor = Substitute.For<IGraphQueryExecutor>();
        _accessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        _instanceRepository = Substitute.For<IInstanceRepository>();
        _logger = Substitute.For<ILogger<Api.Utilities.CompletenessCheck>>();
        _dynamicKafkaProducer = Substitute.For<IDynamicKafkaProducer>();
        _guidelineProvider = Substitute.For<IGuidelineProvider>();

        _completenessCheck = new Api.Utilities.CompletenessCheck(
            _cypherQueryExecutor,
            _accessRightsFetcher,
            _instanceRepository,
            _logger,
            _dynamicKafkaProducer,
            _guidelineProvider
        );
    }

    #region Edge Case 1: InstanceWithEmptyId

    [Fact]
    public async Task InstanceWithEmptyId_ShouldHandleGracefully()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.InstanceWithEmptyId();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync("", useCaseId);

        // Assert
        result.Should().BeFalse("empty instance ID should be handled gracefully");
    }

    #endregion

    #region Edge Case 2: InstanceWithNullClassificationId

    [Fact]
    public async Task InstanceWithNullClassificationId_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.InstanceWithNullClassificationId();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("null classification ID should result in incomplete");
    }

    #endregion

    #region Edge Case 3: InstanceWithNullProperties

    [Fact]
    public async Task InstanceWithNullProperties_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.InstanceWithNullProperties();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("null properties dictionary should result in incomplete");
    }

    #endregion

    #region Edge Case 4: VeryDeepGraph

    [Fact]
    public async Task VeryDeepGraph_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.VeryDeepGraph();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("deep graph up to 10 levels should be traversed correctly");
        instances.Should().HaveCount(10);
    }

    #endregion

    #region Edge Case 5: VeryWideGraph

    [Fact]
    public async Task VeryWideGraph_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.VeryWideGraph();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("wide graph with 50 children should be complete");
        instances.Should().HaveCount(51, "1 building + 50 floors");

        var building = instances.First(i => i.ClassificationId == EdgeCaseScenarios.BuildingClassId);
        building.Relations.Should().HaveCount(50);
    }

    #endregion

    #region Edge Case 6: SpecialCharactersInProperties

    [Fact]
    public async Task SpecialCharactersInProperties_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.SpecialCharactersInProperties();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("special characters in properties should not break completeness check");

        var building = instances[0];
        building.Properties[EdgeCaseScenarios.NameProperty].Should().Contain("<>&");
        building.Properties[EdgeCaseScenarios.DescriptionProperty].Should().Contain("äöüß");
    }

    #endregion

    #region Edge Case 7: DuplicateInstances

    [Fact]
    public async Task DuplicateInstances_ShouldHandleGracefully()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.DuplicateInstances();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("duplicate instances should be handled (last one wins or first one wins)");
    }

    #endregion

    #region Edge Case 8: BrokenRelationships

    [Fact]
    public async Task BrokenRelationships_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.BrokenRelationships();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("broken relationships should result in incomplete (Floor not found)");
    }

    #endregion

    #region Edge Case 9: NonReadAccessRights

    [Fact]
    public async Task NonReadAccessRights_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.NonReadAccessRights();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("Write permissions should be ignored for completeness");
        accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.Write);
    }

    #endregion

    #region Edge Case 10: MixedAccessRights

    [Fact]
    public async Task MixedAccessRights_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.MixedAccessRights();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("only Read properties should be required, Write properties ignored");

        accessRights.Should().Contain(ar => ar.Right == PropertyRight.Read);
        accessRights.Should().Contain(ar => ar.Right == PropertyRight.Write);

        // Description property is missing but has Write right, so it shouldn't matter
        var building = instances[0];
        building.Properties.Should().ContainKey(EdgeCaseScenarios.NameProperty);
        building.Properties.Should().NotContainKey(EdgeCaseScenarios.DescriptionProperty);
    }

    #endregion

    #region Edge Case 11: VeryLongPropertyValues

    [Fact]
    public async Task VeryLongPropertyValues_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.VeryLongPropertyValues();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("very long property values should be handled correctly");

        var building = instances[0];
        building.Properties[EdgeCaseScenarios.DescriptionProperty].Should().HaveLength(10000);
    }

    #endregion

    #region Edge Case 12: SelfReferencingInstance

    [Fact]
    public async Task SelfReferencingInstance_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.SelfReferencingInstance();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("self-referencing instances should not cause infinite loops");

        var building = instances[0];
        building.Relations.Should().Contain(r => r.SubjectId == building.Id && r.ObjectId == building.Id);
    }

    #endregion

    #region Edge Case 13: WhitespaceOnlyProperties

    // TODO: Decide if whitespace-only properties should be treated as incomplete
    //[Fact]
    //public async Task WhitespaceOnlyProperties_ShouldBeIncomplete()
    //{
    //    // Arrange
    //    var (instances, accessRights, useCaseId) = EdgeCaseScenarios.WhitespaceOnlyProperties();
    //    var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

    //    // Act
    //    var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

    //    // Assert
    //    // Note: Current implementation treats whitespace as valid value
    //    // If this should be incomplete, CompletenessCheck needs to add string.IsNullOrWhiteSpace check
    //    result.Should().BeFalse("whitespace-only properties should be treated as incomplete");
    //}

    #endregion

    #region Edge Case 14: CaseSensitiveClassificationIds

    [Fact]
    public async Task CaseSensitiveClassificationIds_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.CaseSensitiveClassificationIds();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("classification ID matching should be case-sensitive");

        var building = instances[0];
        building.ClassificationId.Should().NotBe(EdgeCaseScenarios.BuildingClassId);
        building.ClassificationId.Should().Be(EdgeCaseScenarios.BuildingClassId.ToUpper());
    }

    #endregion

    #region Edge Case 15: DuplicateAccessRights

    [Fact]
    public async Task DuplicateAccessRights_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.DuplicateAccessRights();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("duplicate access rights should not affect completeness");
        accessRights.Should().HaveCount(3, "there are 3 duplicate entries");
    }

    #endregion

    #region Edge Case 16: DisconnectedGraphIslands

    [Fact]
    public async Task DisconnectedGraphIslands_CompleteIsland_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.DisconnectedGraphIslands();

        // Setup mocks with only the complete island (building-island-1 + floor-island-1)
        var completeIsland = instances.Where(i => i.Id.Contains("island-1")).ToList();
        var completenessCheck = SetupMocks(completeIsland, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync("building-island-1", useCaseId);

        // Assert
        result.Should().BeTrue("complete disconnected island should be complete");
    }

    [Fact]
    public async Task DisconnectedGraphIslands_IncompleteIsland_MissingClass_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.DisconnectedGraphIslands();

        // Setup mocks with only the incomplete island (building-island-2, no floor)
        var incompleteIsland = instances.Where(i => i.Id == "building-island-2").ToList();
        var completenessCheck = SetupMocks(incompleteIsland, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync("building-island-2", useCaseId);

        // Assert
        result.Should().BeFalse("island missing Floor class should be incomplete");
    }

    [Fact]
    public async Task DisconnectedGraphIslands_IncompleteIsland_MissingProperty_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = EdgeCaseScenarios.DisconnectedGraphIslands();

        // Setup mocks with only the incomplete island (building-island-3 + floor-island-3)
        var incompleteIsland = instances.Where(i => i.Id.Contains("island-3")).ToList();
        var completenessCheck = SetupMocks(incompleteIsland, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync("building-island-3", useCaseId);

        // Assert
        result.Should().BeFalse("island with missing Floor property should be incomplete");

        var floor = incompleteIsland.First(i => i.Id == "floor-island-3");
        floor.Properties.Should().NotContainKey(EdgeCaseScenarios.NameProperty);
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
