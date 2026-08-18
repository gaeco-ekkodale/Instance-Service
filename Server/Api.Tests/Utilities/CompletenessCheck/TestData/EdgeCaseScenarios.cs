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
using InstanceService.Models.Enum;

namespace InstanceService.Api.Tests.Utilities.CompletenessCheck.TestData;

/// <summary>
/// Provides edge case test data scenarios for completeness check testing
/// Covers boundary conditions, null/empty values, and unusual configurations
/// </summary>
public static class EdgeCaseScenarios
{
    // Classification IDs
    public const string BuildingClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcBuilding";
    public const string FloorClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcBuildingStorey";
    public const string RoomClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcSpace";
    public const string WallClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWall";

    // Property names
    public const string NameProperty = "Name";
    public const string DescriptionProperty = "Description";
    public const string HeightProperty = "Height";
    public const string AreaProperty = "Area";

    // Use Case IDs
    public const string EdgeCaseUseCaseId = "e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1";
    public const string EmptyUseCaseId = "00000000-0000-0000-0000-000000000000";

    // Relation label
    public const string ContainsRelation = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Contains";

    /// <summary>
    /// Edge Case 1: Instance with null/empty ID
    /// Tests handling of invalid instance IDs
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InstanceWithEmptyId()
    {
        var buildingId = "";
        var floorId = "floor-edge-1";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building with empty ID",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" }
                },
                Relations = new List<InstanceRelation>()
            },
            new Instance
            {
                Id = floorId,
                Name = "Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, FloorClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 2: Instance with null classification ID
    /// Tests handling of invalid classification IDs
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InstanceWithNullClassificationId()
    {
        var buildingId = "building-edge-2";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = null!,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 3: Instance with null properties dictionary
    /// Tests handling of null property collections
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InstanceWithNullProperties()
    {
        var buildingId = "building-edge-3";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = null!,
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 4: Very deep graph (max depth at 255 levels)
    /// Tests the path depth limit in Neo4j query
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) VeryDeepGraph()
    {
        var instances = new List<Instance>();
        var depth = 10; // Using 10 for practical testing (255 would be too large)

        for (int i = 0; i < depth; i++)
        {
            var currentId = $"instance-deep-{i}";
            var nextId = i < depth - 1 ? $"instance-deep-{i + 1}" : null;
            var classId = i % 2 == 0 ? BuildingClassId : FloorClassId;

            var relations = new List<InstanceRelation>();
            if (nextId != null)
            {
                relations.Add(new InstanceRelation { SubjectId = currentId, ObjectId = nextId, PredicateUri = ContainsRelation });
            }

            instances.Add(new Instance
            {
                Id = currentId,
                Name = $"Instance Level {i}",
                ClassificationId = classId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, $"Instance Level {i}" }
                },
                Relations = relations
            });
        }

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, FloorClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 5: Very wide graph (many children)
    /// Tests performance with many parallel relationships
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) VeryWideGraph()
    {
        var buildingId = "building-wide";
        var childCount = 50; // 50 direct children

        var building = new Instance
        {
            Id = buildingId,
            Name = "Wide Building",
            ClassificationId = BuildingClassId,
            Properties = new Dictionary<string, string>
            {
                { NameProperty, "Wide Building" }
            },
            Relations = new List<InstanceRelation>()
        };

        var instances = new List<Instance> { building };

        for (int i = 0; i < childCount; i++)
        {
            var floorId = $"floor-wide-{i}";
            building.Relations.Add(new InstanceRelation
            {
                SubjectId = buildingId,
                ObjectId = floorId,
                PredicateUri = ContainsRelation
            });

            instances.Add(new Instance
            {
                Id = floorId,
                Name = $"Floor {i}",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, $"Floor {i}" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            });
        }

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, FloorClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 6: Special characters in property values
    /// Tests handling of special characters in property values
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) SpecialCharactersInProperties()
    {
        var buildingId = "building-special";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building with special chars",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building <>&\"'`\n\t\r" },
                    { DescriptionProperty, "Unicode: äöüß€™®©" },
                    { HeightProperty, "25.5" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, HeightProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 7: Duplicate instances (same ID appears twice)
    /// Tests handling of duplicate instance IDs
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) DuplicateInstances()
    {
        var buildingId = "building-duplicate";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 1",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 1" }
                },
                Relations = new List<InstanceRelation>()
            },
            new Instance
            {
                Id = buildingId, // Same ID!
                Name = "Building 2",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 2" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 8: Broken relationships (references non-existent instances)
    /// Tests handling of invalid relationship references
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) BrokenRelationships()
    {
        var buildingId = "building-broken";
        var nonExistentId = "non-existent-floor";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = nonExistentId, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, FloorClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 9: Access rights with Write/Execute permissions (not Read)
    /// Tests that only Read permissions are checked for completeness
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) NonReadAccessRights()
    {
        var buildingId = "building-write-access";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" },
                    { DescriptionProperty, "Has value" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            // Using Write instead of Read
            new AccessRight
            {
                Id = Guid.NewGuid().ToString(),
                Name = NameProperty,
                GuidelineClassificationId = BuildingClassId,
                UserGroupId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                UseCaseId = Guid.Parse(EdgeCaseUseCaseId),
                GuidlineClassificationPropertyId = $"{BuildingClassId}/prop/{NameProperty}",
                Right = PropertyRight.Write
            },
            new AccessRight
            {
                Id = Guid.NewGuid().ToString(),
                Name = DescriptionProperty,
                GuidelineClassificationId = BuildingClassId,
                UserGroupId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                UseCaseId = Guid.Parse(EdgeCaseUseCaseId),
                GuidlineClassificationPropertyId = $"{BuildingClassId}/prop/{DescriptionProperty}",
                Right = PropertyRight.Write
            }
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 10: Mixed access rights (some Read, some Write)
    /// Tests that only Read properties are required for completeness
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) MixedAccessRights()
    {
        var buildingId = "building-mixed-access";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" }
                    // DescriptionProperty missing, but it's Write-only
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            // Read access right
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            // Write access right
            new AccessRight
            {
                Id = Guid.NewGuid().ToString(),
                Name = DescriptionProperty,
                GuidelineClassificationId = BuildingClassId,
                UserGroupId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                UseCaseId = Guid.Parse(EdgeCaseUseCaseId),
                GuidlineClassificationPropertyId = $"{BuildingClassId}/prop/{DescriptionProperty}",
                Right = PropertyRight.Write
            }
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 11: Very long property values
    /// Tests handling of large string values
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) VeryLongPropertyValues()
    {
        var buildingId = "building-long-props";
        var longString = new string('A', 10000); // 10,000 character string

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" },
                    { DescriptionProperty, longString }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, DescriptionProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 12: Graph with self-referencing instance
    /// Tests handling of instances that reference themselves
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) SelfReferencingInstance()
    {
        var buildingId = "building-self-ref";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Self-referencing Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Self-referencing Building" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = buildingId, PredicateUri = "SelfReference" }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 13: Whitespace-only property values
    /// Tests that whitespace-only values are treated as incomplete
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) WhitespaceOnlyProperties()
    {
        var buildingId = "building-whitespace";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "   " }, // Whitespace only
                    { DescriptionProperty, "\t\n\r" } // Whitespace only
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, DescriptionProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 14: Case-sensitive classification IDs
    /// Tests that classification ID matching is case-sensitive
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) CaseSensitiveClassificationIds()
    {
        var buildingId = "building-case-sensitive";
        var wrongCaseClassId = BuildingClassId.ToUpper();

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = wrongCaseClassId, // Wrong case
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 15: Multiple access rights for same property
    /// Tests handling of duplicate access right entries
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) DuplicateAccessRights()
    {
        var buildingId = "building-dup-access";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty), // Duplicate
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty)  // Duplicate
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Edge Case 16: Disconnected graph (islands)
    /// Multiple subgraphs with no connections between them, some complete, some not
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) DisconnectedGraphIslands()
    {
        var building1Id = "building-island-1";
        var floor1Id = "floor-island-1";
        
        var building2Id = "building-island-2";
        
        var building3Id = "building-island-3";
        var floor3Id = "floor-island-3";

        var instances = new List<Instance>
        {
            // Complete island 1
            new Instance
            {
                Id = building1Id,
                Name = "Building 1",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 1" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building1Id, ObjectId = floor1Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor1Id,
                Name = "Floor 1",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor 1" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building1Id, ObjectId = floor1Id, PredicateUri = ContainsRelation }
                }
            },
            // Incomplete island 2 (missing floor)
            new Instance
            {
                Id = building2Id,
                Name = "Building 2",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 2" }
                },
                Relations = new List<InstanceRelation>()
            },
            // Incomplete island 3 (floor missing property)
            new Instance
            {
                Id = building3Id,
                Name = "Building 3",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 3" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building3Id, ObjectId = floor3Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor3Id,
                Name = "Floor 3",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    // Missing NameProperty!
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building3Id, ObjectId = floor3Id, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(EdgeCaseUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(EdgeCaseUseCaseId, FloorClassId, NameProperty)
        };

        return (instances, accessRights, EdgeCaseUseCaseId);
    }

    /// <summary>
    /// Helper method to create access rights with consistent structure
    /// </summary>
    private static AccessRight CreateAccessRight(string useCaseId, string classificationId, string propertyName)
    {
        return new AccessRight
        {
            Id = Guid.NewGuid().ToString(),
            Name = propertyName,
            GuidelineClassificationId = classificationId,
            UserGroupId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UseCaseId = Guid.Parse(useCaseId),
            GuidlineClassificationPropertyId = $"{classificationId}/prop/{propertyName}",
            Right = PropertyRight.Read
        };
    }
}
