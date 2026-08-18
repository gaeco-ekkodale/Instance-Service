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
/// Comprehensive tests for CompletenessCheck using BuildingScenarios test data
/// Demonstrates testing with all three AccessRight variants
/// </summary>
public class CompletenessCheckTests
{
    private readonly IGraphQueryExecutor _cypherQueryExecutor;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IInstanceRepository _instanceRepository;
    private readonly ILogger<Api.Utilities.CompletenessCheck> _logger;
    private readonly IDynamicKafkaProducer _dynamicKafkaProducer;
    private readonly IGuidelineProvider _guidelineProvider;
    private readonly Api.Utilities.CompletenessCheck _completenessCheck;

    public CompletenessCheckTests()
    {
        // Setup mocks using NSubstitute
        _cypherQueryExecutor = Substitute.For<IGraphQueryExecutor>();
        _accessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        _instanceRepository = Substitute.For<IInstanceRepository>();
        _logger = Substitute.For<ILogger<Api.Utilities.CompletenessCheck>>();
        _dynamicKafkaProducer = Substitute.For<IDynamicKafkaProducer>();
        _guidelineProvider = Substitute.For<IGuidelineProvider>();

        // Create system under test
        _completenessCheck = new Api.Utilities.CompletenessCheck(
            _cypherQueryExecutor,
            _accessRightsFetcher,
            _instanceRepository,
            _logger,
            _dynamicKafkaProducer,
            _guidelineProvider
        );
    }

    #region CompleteSimpleGraph Tests

    /// <summary>
    /// Test: CompleteSimpleGraph with AllRead variant
    /// Expected: Complete (all Read properties are present)
    /// </summary>
    [Fact]
    public async Task CompleteSimpleGraph_WithAllReadVariant_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph(
            BuildingScenarios.AccessRightVariant.AllRead
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("all required Read properties are present and filled");

        // Verify access rights were fetched
        //await _accessRightsFetcher.Received(1).GetAccessRightsAsync();

        // Verify instances were retrieved from repository
        await _instanceRepository.Received().GetInstance(instances[0].Id);
    }

    /// <summary>
    /// Test: CompleteSimpleGraph with AllNone variant
    /// Expected: Complete (properties are ignored, only classifications matter)
    /// </summary>
    [Fact]
    public async Task CompleteSimpleGraph_WithAllNoneVariant_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph(
            BuildingScenarios.AccessRightVariant.AllNone
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("all required classifications are present, properties are ignored (None)");

        // Verify access rights were fetched
        //await _accessRightsFetcher.Received(1).GetAccessRightsAsync();

        // Verify all AccessRights have None
        accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.None);
    }

    /// <summary>
    /// Test: CompleteSimpleGraph with Mixed variant
    /// Expected: Complete (only Read properties are required, None properties are optional)
    /// </summary>
    [Fact]
    public async Task CompleteSimpleGraph_WithMixedVariant_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph(
            BuildingScenarios.AccessRightVariant.Mixed
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("all required Read properties are present (None properties are optional)");

        // Verify the correct access rights variant was used
        accessRights.Should().Contain(ar => ar.Right == PropertyRight.Read, "Mixed variant should have Read rights");
        accessRights.Should().Contain(ar => ar.Right == PropertyRight.None, "Mixed variant should have None rights");
    }

    /// <summary>
    /// Theory test: CompleteSimpleGraph with all variants using test data factory
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAccessRightVariants))]
    public async Task CompleteSimpleGraph_WithAllVariants_ShouldBeComplete(
        BuildingScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph(variant);

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"CompleteSimpleGraph should be complete with {variant} variant");

        // Verify behavior based on variant
        switch (variant)
        {
            case BuildingScenarios.AccessRightVariant.AllRead:
                accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.Read,
                    "AllRead variant should only have Read rights");
                break;
            case BuildingScenarios.AccessRightVariant.AllNone:
                accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.None,
                    "AllNone variant should only have None rights");
                break;
            case BuildingScenarios.AccessRightVariant.Mixed:
                accessRights.Should().Contain(ar => ar.Right == PropertyRight.Read,
                    "Mixed variant should have Read rights");
                accessRights.Should().Contain(ar => ar.Right == PropertyRight.None,
                    "Mixed variant should have None rights");
                break;
        }
    }

    #endregion

    #region IncompleteGraphMissingProperty Tests

    /// <summary>
    /// Test: IncompleteGraphMissingProperty with AllRead variant
    /// Expected: Incomplete (required Read property is missing)
    /// </summary>
    [Fact]
    public async Task IncompleteGraphMissingProperty_WithAllReadVariant_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingProperty(
            BuildingScenarios.AccessRightVariant.AllRead
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Floor is missing required Height property (Read)");

        // Verify that the missing property was from the Floor classification
        var floorInstance = instances.First(i => i.ClassificationId == BuildingScenarios.FloorClassId);
        floorInstance.Properties.Should().NotContainKey(BuildingScenarios.HeightProperty,
            "Floor should be missing the Height property in this test scenario");
    }

    /// <summary>
    /// Test: IncompleteGraphMissingProperty with AllNone variant
    /// Expected: Complete (properties are ignored with None)
    /// </summary>
    [Fact]
    public async Task IncompleteGraphMissingProperty_WithAllNoneVariant_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingProperty(
            BuildingScenarios.AccessRightVariant.AllNone
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("properties are ignored with None variant, only classifications matter");

        // Verify all properties have None right
        accessRights.Should().OnlyContain(ar => ar.Right == PropertyRight.None);
    }

    /// <summary>
    /// Test: IncompleteGraphMissingProperty with Mixed variant
    /// Expected: Complete (missing property has None right, so it's optional)
    /// </summary>
    [Fact]
    public async Task IncompleteGraphMissingProperty_WithMixedVariant_ShouldBeComplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingProperty(
            BuildingScenarios.AccessRightVariant.Mixed
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue("Floor.Height has None right in Mixed variant, so it's optional");

        // Verify that Height property has None right
        var heightAccessRight = accessRights.First(ar =>
            ar.GuidelineClassificationId == BuildingScenarios.FloorClassId &&
            ar.Name == BuildingScenarios.HeightProperty);
        heightAccessRight.Right.Should().Be(PropertyRight.None,
            "Height property should have None right in Mixed variant");
    }

    /// <summary>
    /// Theory test: IncompleteGraphMissingProperty with different expected results per variant
    /// </summary>
    [Theory]
    [InlineData(BuildingScenarios.AccessRightVariant.AllRead, false)]  // Incomplete - property required
    [InlineData(BuildingScenarios.AccessRightVariant.AllNone, true)]   // Complete - properties ignored
    [InlineData(BuildingScenarios.AccessRightVariant.Mixed, true)]     // Complete - Height is None
    public async Task IncompleteGraphMissingProperty_WithVariant_ShouldMatchExpectedCompleteness(
        BuildingScenarios.AccessRightVariant variant,
        bool expectedComplete)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingProperty(variant);

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().Be(expectedComplete,
            $"variant {variant} should result in {(expectedComplete ? "complete" : "incomplete")} graph");

        // Additional verification based on variant
        if (variant == BuildingScenarios.AccessRightVariant.AllRead)
        {
            var floorInstance = instances.First(i => i.ClassificationId == BuildingScenarios.FloorClassId);
            floorInstance.Properties.Should().NotContainKey(BuildingScenarios.HeightProperty);
        }
    }

    #endregion

    #region IncompleteGraphMissingClass Tests

    /// <summary>
    /// Test: IncompleteGraphMissingClass should always be incomplete (missing required classification)
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAccessRightVariants))]
    public async Task IncompleteGraphMissingClass_WithAnyVariant_ShouldAlwaysBeIncomplete(
        BuildingScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = BuildingScenarios.IncompleteGraphMissingClass(variant);

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("Floor classification is missing, required regardless of property rights");

        // Verify Floor class is not in the instances
        instances.Should().NotContain(i => i.ClassificationId == BuildingScenarios.FloorClassId,
            "Floor classification should be missing from instances");

        // Verify Floor is required by access rights
        accessRights.Should().Contain(ar => ar.GuidelineClassificationId == BuildingScenarios.FloorClassId,
            "Floor classification should be required by access rights");
    }

    #endregion

    #region Empty and Edge Case Tests

    /// <summary>
    /// Test: Empty instance ID should return false
    /// </summary>
    [Fact]
    public async Task IsUseCaseCompleteAsync_WithEmptyInstanceId_ShouldReturnFalse()
    {
        // Arrange
        var (_, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph();
        _accessRightsFetcher.GetAccessRightsAsync()
            .Returns(Task.FromResult<IEnumerable<AccessRight>>(accessRights));

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync("", useCaseId);

        // Assert
        result.Should().BeFalse("empty instance ID should result in false");
    }

    /// <summary>
    /// Test: Empty use case ID should return false
    /// </summary>
    [Fact]
    public async Task IsUseCaseCompleteAsync_WithEmptyUseCaseId_ShouldReturnFalse()
    {
        // Arrange
        var (instances, accessRights, _) = BuildingScenarios.CompleteSimpleGraph();
        _accessRightsFetcher.GetAccessRightsAsync()
            .Returns(Task.FromResult<IEnumerable<AccessRight>>(accessRights));

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, "");

        // Assert
        result.Should().BeFalse("empty use case ID should result in false");
    }

    /// <summary>
    /// Test: Non-existent instance ID should return false
    /// </summary>
    [Fact]
    public async Task IsUseCaseCompleteAsync_WithNonExistentInstance_ShouldReturnFalse()
    {
        // Arrange
        var (_, accessRights, useCaseId) = BuildingScenarios.CompleteSimpleGraph();
        _accessRightsFetcher.GetAccessRightsAsync()
            .Returns(Task.FromResult<IEnumerable<AccessRight>>(accessRights));
        _instanceRepository.GetInstance(Arg.Any<string>())
            .Returns(Task.FromResult<Instance?>(null));

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync("non-existent-id", useCaseId);

        // Assert
        result.Should().BeFalse("non-existent instance should result in false");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Theory data provider for AccessRight variants
    /// </summary>
    public static IEnumerable<object[]> GetAccessRightVariants()
    {
        yield return new object[] { BuildingScenarios.AccessRightVariant.AllRead };
        yield return new object[] { BuildingScenarios.AccessRightVariant.AllNone };
        yield return new object[] { BuildingScenarios.AccessRightVariant.Mixed };
    }

    /// <summary>
    /// Sets up mocks for the test with the given instances and access rights
    /// </summary>
    private void SetupMocks(List<Instance> instances, List<AccessRight> accessRights, string useCaseId)
    {
        // Mock access rights fetcher
        _accessRightsFetcher.GetAccessRightsAsync()
            .Returns(Task.FromResult<IEnumerable<AccessRight>>(accessRights));

        // Mock instance repository - return instances by ID
        foreach (var instance in instances)
        {
            _instanceRepository.GetInstance(instance.Id)
                .Returns(Task.FromResult<Instance?>(instance));
        }

        // Mock Cypher Query Executor - this is now much simpler!
        _cypherQueryExecutor.ExecuteCompletenessQueryAsync(
                Arg.Any<string>(),
                Arg.Any<List<string>>())
            .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

        _cypherQueryExecutor.FindCandidateInstancesAsync(Arg.Any<List<string>>())
            .Returns(Task.FromResult<IEnumerable<Instance>>(instances));
    }

    #endregion

    #region Integration-style Tests Using TestDataFactory

    /// <summary>
    /// Example of using TestDataFactory for parameterized tests
    /// </summary>
    [Theory]
    [MemberData(nameof(TestDataFactory.GetAccessRightVariants), MemberType = typeof(TestDataFactory))]
    public async Task CompleteSimpleGraph_UsingTestDataFactory_ShouldBeComplete(
        BuildingScenarios.AccessRightVariant variant)
    {
        // Arrange
        var (instances, accessRights, useCaseId) = TestDataFactory.GetSimpleCompleteScenario(variant);

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeTrue($"simple complete scenario should always be complete with {variant}");
    }

    /// <summary>
    /// Test using TestDataFactory for incomplete scenario
    /// </summary>
    [Fact]
    public async Task IncompleteScenario_UsingTestDataFactory_WithAllRead_ShouldBeIncomplete()
    {
        // Arrange
        var (instances, accessRights, useCaseId) = TestDataFactory.GetSimpleIncompleteScenario(
            BuildingScenarios.AccessRightVariant.AllRead
        );

        SetupMocks(instances, accessRights, useCaseId);

        // Act
        var result = await _completenessCheck.IsUseCaseCompleteAsync(instances[0].Id, useCaseId);

        // Assert
        result.Should().BeFalse("incomplete scenario with AllRead should be incomplete");
    }

    #endregion
}
