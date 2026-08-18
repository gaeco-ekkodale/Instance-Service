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
using Xunit;

namespace InstanceService.Api.Tests.Utilities.CompletenessCheck;

/// <summary>
/// Tests for all StateTransitionScenarios test data
/// Tests how completeness changes as data and requirements evolve
/// </summary>
public class StateTransitionScenariosTests
{
    #region Transition 1: IncompleteToComplete (Property Added)

    [Fact]
    public async Task IncompleteToComplete_InitialState_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.IncompleteToComplete.InitialState();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Building is missing required Description property");
        
        var building = instances.First(i => i.ClassificationId == StateTransitionScenarios.BuildingClassId);
        building.Properties.Should().NotContainKey(StateTransitionScenarios.DescriptionProperty);
    }

    [Fact]
    public async Task IncompleteToComplete_AfterPropertyAdded_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.IncompleteToComplete.AfterPropertyAdded();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("all required properties are now present");
        
        var building = instances.First(i => i.ClassificationId == StateTransitionScenarios.BuildingClassId);
        building.Properties.Should().ContainKey(StateTransitionScenarios.DescriptionProperty);
        building.Properties[StateTransitionScenarios.DescriptionProperty].Should().Be("Now complete!");
    }

    [Fact]
    public async Task IncompleteToComplete_Transition_ShouldChangeFromIncompleteToComplete()
    {
        // Arrange - Initial incomplete state
        var (instancesBefore, accessRights, useCaseId) = StateTransitionScenarios.IncompleteToComplete.InitialState();
        var completenessCheckBefore = SetupMocks(instancesBefore, accessRights, useCaseId);
        
        // Act - Check initial state
        var resultBefore = await completenessCheckBefore.IsUseCaseCompleteAsync(instancesBefore[0].Id, useCaseId);

        // Arrange - After property added
        var (instancesAfter, _, _) = StateTransitionScenarios.IncompleteToComplete.AfterPropertyAdded();
        var completenessCheckAfter = SetupMocks(instancesAfter, accessRights, useCaseId);
        
        // Act - Check state after transition
        var resultAfter = await completenessCheckAfter.IsUseCaseCompleteAsync(instancesAfter[0].Id, useCaseId);

        // Assert - Verify transition
        resultBefore.Should().BeFalse("initially incomplete");
        resultAfter.Should().BeTrue("complete after property added");
    }

    #endregion

    #region Transition 2: IncompleteToCompleteWithNewInstance (Instance Added)

    [Fact]
    public async Task IncompleteToCompleteWithNewInstance_InitialState_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.IncompleteToCompleteWithNewInstance.InitialState();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Floor instance is missing");
        instances.Should().ContainSingle();
        instances.Should().NotContain(i => i.ClassificationId == StateTransitionScenarios.FloorClassId);
    }

    [Fact]
    public async Task IncompleteToCompleteWithNewInstance_AfterInstanceAdded_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.IncompleteToCompleteWithNewInstance.AfterInstanceAdded();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("Floor instance has been added");
        instances.Should().HaveCount(2);
        instances.Should().ContainSingle(i => i.ClassificationId == StateTransitionScenarios.BuildingClassId);
        instances.Should().ContainSingle(i => i.ClassificationId == StateTransitionScenarios.FloorClassId);
    }

    #endregion

    #region Transition 3: CompleteToIncomplete (Property Removed)

    [Fact]
    public async Task CompleteToIncomplete_InitialState_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.CompleteToIncomplete.InitialState();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("all required data is present");
        
        var building = instances.First(i => i.ClassificationId == StateTransitionScenarios.BuildingClassId);
        building.Properties.Should().ContainKey(StateTransitionScenarios.DescriptionProperty);
    }

    [Fact]
    public async Task CompleteToIncomplete_AfterPropertyRemoved_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.CompleteToIncomplete.AfterPropertyRemoved();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Description property has been removed");
        
        var building = instances.First(i => i.ClassificationId == StateTransitionScenarios.BuildingClassId);
        building.Properties.Should().NotContainKey(StateTransitionScenarios.DescriptionProperty);
    }

    [Fact]
    public async Task CompleteToIncomplete_Transition_ShouldChangeFromCompleteToIncomplete()
    {
        // Arrange - Initial complete state
        var (instancesBefore, accessRights, useCaseId) = StateTransitionScenarios.CompleteToIncomplete.InitialState();
        var completenessCheckBefore = SetupMocks(instancesBefore, accessRights, useCaseId);
        
        // Act - Check initial state
        var resultBefore = await completenessCheckBefore.IsUseCaseCompleteAsync(instancesBefore[0].Id, useCaseId);

        // Arrange - After property removed
        var (instancesAfter, _, _) = StateTransitionScenarios.CompleteToIncomplete.AfterPropertyRemoved();
        var completenessCheckAfter = SetupMocks(instancesAfter, accessRights, useCaseId);
        
        // Act - Check state after transition
        var resultAfter = await completenessCheckAfter.IsUseCaseCompleteAsync(instancesAfter[0].Id, useCaseId);

        // Assert - Verify transition
        resultBefore.Should().BeTrue("initially complete");
        resultAfter.Should().BeFalse("incomplete after property removed");
    }

    #endregion

    #region Transition 4: AccessRightsIncrease (Requirements Added)

    [Fact]
    public async Task AccessRightsIncrease_InitialState_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.AccessRightsIncrease.InitialState();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("minimal requirements are met");
        accessRights.Should().HaveCount(2, "only Name properties are required");
    }

    [Fact]
    public async Task AccessRightsIncrease_AfterAccessRightsAdded_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.AccessRightsIncrease.AfterAccessRightsAdded();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("new requirements (Description, Height) are not met by existing data");
        accessRights.Should().HaveCount(4, "additional requirements have been added");
        
        // Verify data hasn't changed, only requirements
        var building = instances.First(i => i.ClassificationId == StateTransitionScenarios.BuildingClassId);
        building.Properties.Should().NotContainKey(StateTransitionScenarios.DescriptionProperty);
    }

    [Fact]
    public async Task AccessRightsIncrease_Transition_RequirementIncreaseCausesIncompleteness()
    {
        // Arrange - Initial state with minimal requirements
        var (instances, accessRightsBefore, useCaseId) = StateTransitionScenarios.AccessRightsIncrease.InitialState();
        var completenessCheckBefore = SetupMocks(instances, accessRightsBefore, useCaseId);
        
        // Act - Check with minimal requirements
        var resultBefore = await completenessCheckBefore.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Arrange - Same data but more requirements
        var (_, accessRightsAfter, _) = StateTransitionScenarios.AccessRightsIncrease.AfterAccessRightsAdded();
        var completenessCheckAfter = SetupMocks(instances, accessRightsAfter, useCaseId);
        
        // Act - Check with increased requirements
        var resultAfter = await completenessCheckAfter.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert - Verify transition
        resultBefore.Should().BeTrue("complete with minimal requirements");
        resultAfter.Should().BeFalse("incomplete when requirements increase");
        
        accessRightsBefore.Should().HaveCount(2);
        accessRightsAfter.Should().HaveCount(4);
    }

    #endregion

    #region Transition 5: AccessRightsDecrease (Requirements Removed)

    [Fact]
    public async Task AccessRightsDecrease_InitialState_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.AccessRightsDecrease.InitialState();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Description property is required but missing");
        accessRights.Should().HaveCount(3);
    }

    [Fact]
    public async Task AccessRightsDecrease_AfterAccessRightsRemoved_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.AccessRightsDecrease.AfterAccessRightsRemoved();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("Description requirement has been removed");
        accessRights.Should().HaveCount(2, "Description requirement removed");
    }

    [Fact]
    public async Task AccessRightsDecrease_Transition_RequirementDecreaseCausesCompleteness()
    {
        // Arrange - Initial state with Description requirement
        var (instances, accessRightsBefore, useCaseId) = StateTransitionScenarios.AccessRightsDecrease.InitialState();
        var completenessCheckBefore = SetupMocks(instances, accessRightsBefore, useCaseId);
        
        // Act - Check with Description requirement
        var resultBefore = await completenessCheckBefore.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Arrange - Same data but fewer requirements
        var (_, accessRightsAfter, _) = StateTransitionScenarios.AccessRightsDecrease.AfterAccessRightsRemoved();
        var completenessCheckAfter = SetupMocks(instances, accessRightsAfter, useCaseId);
        
        // Act - Check with decreased requirements
        var resultAfter = await completenessCheckAfter.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert - Verify transition
        resultBefore.Should().BeFalse("incomplete with Description requirement");
        resultAfter.Should().BeTrue("complete when Description requirement removed");
        
        accessRightsBefore.Should().HaveCount(3);
        accessRightsAfter.Should().HaveCount(2);
    }

    #endregion

    #region Transition 6: PropertyValueUpdate (Empty to Filled)

    [Fact]
    public async Task PropertyValueUpdate_BeforeUpdate_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.PropertyValueUpdate.BeforeUpdate();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Name property has empty value");
        
        var building = instances[0];
        building.Properties[StateTransitionScenarios.NameProperty].Should().BeEmpty();
    }

    [Fact]
    public async Task PropertyValueUpdate_AfterUpdate_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = StateTransitionScenarios.PropertyValueUpdate.AfterUpdate();
        var completenessCheck = SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("Name property now has value");
        
        var building = instances[0];
        building.Properties[StateTransitionScenarios.NameProperty].Should().NotBeEmpty();
        building.Properties[StateTransitionScenarios.NameProperty].Should().Be("Updated Building Name");
    }

    [Fact]
    public async Task PropertyValueUpdate_Transition_EmptyToFilledMakesComplete()
    {
        // Arrange - Before update (empty value)
        var (instancesBefore, accessRights, useCaseId) = StateTransitionScenarios.PropertyValueUpdate.BeforeUpdate();
        var completenessCheckBefore = SetupMocks(instancesBefore, accessRights, useCaseId);
        
        // Act - Check before update
        var resultBefore = await completenessCheckBefore.IsUseCaseCompleteAsync(instancesBefore[0].Id, useCaseId);

        // Arrange - After update (filled value)
        var (instancesAfter, _, _) = StateTransitionScenarios.PropertyValueUpdate.AfterUpdate();
        var completenessCheckAfter = SetupMocks(instancesAfter, accessRights, useCaseId);
        
        // Act - Check after update
        var resultAfter = await completenessCheckAfter.IsUseCaseCompleteAsync(instancesAfter[0].Id, useCaseId);

        // Assert - Verify transition
        resultBefore.Should().BeFalse("incomplete with empty property value");
        resultAfter.Should().BeTrue("complete after property value filled");
        
        instancesBefore[0].Properties[StateTransitionScenarios.NameProperty].Should().BeEmpty();
        instancesAfter[0].Properties[StateTransitionScenarios.NameProperty].Should().NotBeEmpty();
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
