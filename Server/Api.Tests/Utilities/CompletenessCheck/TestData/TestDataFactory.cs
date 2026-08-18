// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models;

namespace InstanceService.Api.Tests.Utilities.CompletenessCheck.TestData;

/// <summary>
/// Factory class to provide test data with consistent structure for unit tests
/// Supports xUnit Theory tests with AccessRight variants
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Gets all available AccessRight variants for parameterized tests
    /// </summary>
    public static IEnumerable<object[]> GetAccessRightVariants()
    {
        yield return new object[] { BuildingScenarios.AccessRightVariant.AllRead };
        yield return new object[] { BuildingScenarios.AccessRightVariant.AllNone };
        yield return new object[] { BuildingScenarios.AccessRightVariant.Mixed };
    }

    /// <summary>
    /// Gets all building scenario test cases with all variants
    /// Returns: (ScenarioName, Instances, AccessRights, UseCaseId, Variant, ExpectedComplete)
    /// </summary>
    public static IEnumerable<object[]> GetAllBuildingScenarios()
    {
        var variants = new[]
        {
            BuildingScenarios.AccessRightVariant.AllRead,
            BuildingScenarios.AccessRightVariant.AllNone,
            BuildingScenarios.AccessRightVariant.Mixed
        };

        foreach (var variant in variants)
        {
            // Complete scenarios
            var (instances1, accessRights1, useCase1) = BuildingScenarios.CompleteSimpleGraph(variant);
            yield return new object[]
            {
                nameof(BuildingScenarios.CompleteSimpleGraph),
                instances1,
                accessRights1,
                useCase1,
                variant,
                true // Expected: Complete
            };

            // Incomplete scenarios - property missing
            var (instances2, accessRights2, useCase2) = BuildingScenarios.IncompleteGraphMissingProperty(variant);
            var expectedComplete2 = variant != BuildingScenarios.AccessRightVariant.AllRead; // Only AllRead requires the property
            yield return new object[]
            {
                nameof(BuildingScenarios.IncompleteGraphMissingProperty),
                instances2,
                accessRights2,
                useCase2,
                variant,
                expectedComplete2 // Depends on variant
            };

            // Incomplete scenarios - class missing
            var (instances3, accessRights3, useCase3) = BuildingScenarios.IncompleteGraphMissingClass(variant);
            yield return new object[]
            {
                nameof(BuildingScenarios.IncompleteGraphMissingClass),
                instances3,
                accessRights3,
                useCase3,
                variant,
                false // Always incomplete - missing required class
            };
        }
    }

    /// <summary>
    /// Gets performance scenario test cases
    /// </summary>
    public static IEnumerable<object[]> GetPerformanceScenarios()
    {
        var variants = new[]
        {
            PerformanceScenarios.AccessRightVariant.AllRead,
            PerformanceScenarios.AccessRightVariant.AllNone,
            PerformanceScenarios.AccessRightVariant.Mixed
        };

        foreach (var variant in variants)
        {
            var (instances, accessRights, useCase) = PerformanceScenarios.LargeCompleteGraph(variant);
            yield return new object[]
            {
                nameof(PerformanceScenarios.LargeCompleteGraph),
                instances,
                accessRights,
                useCase,
                variant,
                true // All variants complete - all properties are present
            };
        }
    }

    /// <summary>
    /// Gets a simple complete scenario for basic tests
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) 
        GetSimpleCompleteScenario(BuildingScenarios.AccessRightVariant variant = BuildingScenarios.AccessRightVariant.AllRead)
    {
        return BuildingScenarios.CompleteSimpleGraph(variant);
    }

    /// <summary>
    /// Gets a simple incomplete scenario for basic tests
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) 
        GetSimpleIncompleteScenario(BuildingScenarios.AccessRightVariant variant = BuildingScenarios.AccessRightVariant.AllRead)
    {
        return BuildingScenarios.IncompleteGraphMissingProperty(variant);
    }
}
