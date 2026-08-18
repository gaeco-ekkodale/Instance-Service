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
/// Provides various building-related test data scenarios for completeness check testing
/// Each scenario supports three AccessRight variants: AllRead, AllNone, and Mixed
/// </summary>
public static class BuildingScenarios
{
    // Classification IDs for building elements
    public const string BuildingClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcBuilding";
    public const string FloorClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcBuildingStorey";
    public const string RoomClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcSpace";
    public const string WallClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWall";
    public const string DoorClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcDoor";
    public const string WindowClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWindow";

    // Property names
    public const string NameProperty = "Name";
    public const string DescriptionProperty = "Description";
    public const string HeightProperty = "Height";
    public const string WidthProperty = "Width";
    public const string AreaProperty = "Area";
    public const string MaterialProperty = "Material";
    public const string LoadCapacityProperty = "LoadCapacity";
    public const string FireRatingProperty = "FireRating";

    // Use Case IDs
    public const string ArchitecturalUseCaseId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
    public const string StructuralUseCaseId = "b2c3d4e5-f6a7-8901-bcde-f12345678901";
    public const string FacilityManagementUseCaseId = "c3d4e5f6-a7b8-9012-cdef-123456789012";

    // Relation labels
    public const string ContainsRelation = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Contains";
    public const string ConnectsRelation = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Connects";

    /// <summary>
    /// Enum to specify which AccessRight variant to use
    /// </summary>
    public enum AccessRightVariant
    {
        /// <summary>All properties have PropertyRight.Read - all must be present for completeness</summary>
        AllRead,
        /// <summary>All properties have PropertyRight.None - properties are ignored for completeness</summary>
        AllNone,
        /// <summary>Mixed PropertyRight.Read and PropertyRight.None - only Read properties required</summary>
        Mixed
    }

    /// <summary>
    /// Scenario 1: Complete simple graph - Building with one floor and one room
    /// All required properties are filled
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) CompleteSimpleGraph(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var buildingId = "building-1";
        var floorId = "floor-1";
        var roomId = "room-1";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Main Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Main Building" },
                    { DescriptionProperty, "Office Building" },
                    { HeightProperty, "25.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Ground Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Ground Floor" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 101",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 101" },
                    { AreaProperty, "45.2" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = CreateAccessRightsForSimpleScenario(ArchitecturalUseCaseId, variant);

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 2: Incomplete graph - Missing required property
    /// Building has all properties, but Floor is missing Height property
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) IncompleteGraphMissingProperty(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var buildingId = "building-2";
        var floorId = "floor-2";
        var roomId = "room-2";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 2",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 2" },
                    { DescriptionProperty, "Commercial Building" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "First Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "First Floor" }
                    // Missing HeightProperty!
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 201",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 201" },
                    { AreaProperty, "30.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = CreateAccessRightsForSimpleScenario(ArchitecturalUseCaseId, variant);

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 3: Incomplete graph - Missing required class
    /// Building and Room exist, but Floor is missing
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) IncompleteGraphMissingClass(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var buildingId = "building-3";
        var roomId = "room-3";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 3",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 3" },
                    { DescriptionProperty, "Residential Building" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 301",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 301" },
                    { AreaProperty, "25.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            }
            // Missing Floor instance!
        };

        var accessRights = CreateAccessRightsForSimpleScenario(ArchitecturalUseCaseId, variant);

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 4: Complete complex graph with multiple floors and rooms
    /// Tests handling of larger connected graphs
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) CompleteComplexGraph()
    {
        var buildingId = "building-4";
        var floor1Id = "floor-4-1";
        var floor2Id = "floor-4-2";
        var room1Id = "room-4-1";
        var room2Id = "room-4-2";
        var room3Id = "room-4-3";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Complex Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Complex Building" },
                    { DescriptionProperty, "Multi-floor Office" },
                    { HeightProperty, "30.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floor1Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = buildingId, ObjectId = floor2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor1Id,
                Name = "Ground Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Ground Floor" },
                    { HeightProperty, "4.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floor1Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor1Id, ObjectId = room1Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor1Id, ObjectId = room2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor2Id,
                Name = "First Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "First Floor" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floor2Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor2Id, ObjectId = room3Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room1Id,
                Name = "Room 101",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 101" },
                    { AreaProperty, "50.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor1Id, ObjectId = room1Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room2Id,
                Name = "Room 102",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 102" },
                    { AreaProperty, "45.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor1Id, ObjectId = room2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room3Id,
                Name = "Room 201",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 201" },
                    { AreaProperty, "60.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor2Id, ObjectId = room3Id, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 5: Multiple disconnected complete subgraphs
    /// Two separate buildings, each complete on their own
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) MultipleCompleteSubgraphs()
    {
        var building1Id = "building-5-1";
        var floor1Id = "floor-5-1";
        var room1Id = "room-5-1";
        
        var building2Id = "building-5-2";
        var floor2Id = "floor-5-2";
        var room2Id = "room-5-2";

        var instances = new List<Instance>
        {
            // First complete subgraph
            new Instance
            {
                Id = building1Id,
                Name = "Building A",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building A" },
                    { DescriptionProperty, "First Building" },
                    { HeightProperty, "20.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building1Id, ObjectId = floor1Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor1Id,
                Name = "Floor A1",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor A1" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building1Id, ObjectId = floor1Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor1Id, ObjectId = room1Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room1Id,
                Name = "Room A101",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room A101" },
                    { AreaProperty, "40.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor1Id, ObjectId = room1Id, PredicateUri = ContainsRelation }
                }
            },
            // Second complete subgraph
            new Instance
            {
                Id = building2Id,
                Name = "Building B",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building B" },
                    { DescriptionProperty, "Second Building" },
                    { HeightProperty, "25.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building2Id, ObjectId = floor2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor2Id,
                Name = "Floor B1",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor B1" },
                    { HeightProperty, "4.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building2Id, ObjectId = floor2Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor2Id, ObjectId = room2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room2Id,
                Name = "Room B101",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room B101" },
                    { AreaProperty, "35.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor2Id, ObjectId = room2Id, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 6: Multiple use cases with overlapping classes
    /// Tests that same instances can satisfy different use cases
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> ArchitecturalAccessRights, List<AccessRight> StructuralAccessRights) MultipleUseCasesOverlappingClasses()
    {
        var buildingId = "building-6";
        var floorId = "floor-6";
        var roomId = "room-6";
        var wallId = "wall-6";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Mixed Use Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Mixed Use Building" },
                    { DescriptionProperty, "Multi-purpose Building" },
                    { HeightProperty, "30.0" },
                    { MaterialProperty, "Concrete" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Ground Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Ground Floor" },
                    { HeightProperty, "4.0" },
                    { LoadCapacityProperty, "5000" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = wallId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 1",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 1" },
                    { AreaProperty, "50.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = wallId,
                Name = "Load-bearing Wall",
                ClassificationId = WallClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Load-bearing Wall" },
                    { HeightProperty, "3.8" },
                    { MaterialProperty, "Reinforced Concrete" },
                    { LoadCapacityProperty, "8000" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = wallId, PredicateUri = ContainsRelation }
                }
            }
        };

        var architecturalAccessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        var structuralAccessRights = new List<AccessRight>
        {
            CreateAccessRight(StructuralUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(StructuralUseCaseId, BuildingClassId, MaterialProperty),
            CreateAccessRight(StructuralUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(StructuralUseCaseId, FloorClassId, LoadCapacityProperty),
            CreateAccessRight(StructuralUseCaseId, WallClassId, NameProperty),
            CreateAccessRight(StructuralUseCaseId, WallClassId, MaterialProperty),
            CreateAccessRight(StructuralUseCaseId, WallClassId, LoadCapacityProperty)
        };

        return (instances, architecturalAccessRights, structuralAccessRights);
    }

    /// <summary>
    /// Scenario 7: Empty properties (empty strings)
    /// Tests that empty string properties are treated as incomplete
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) IncompleteGraphEmptyProperty()
    {
        var buildingId = "building-7";
        var floorId = "floor-7";
        var roomId = "room-7";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 7",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 7" },
                    { DescriptionProperty, "" }, // Empty string!
                    { HeightProperty, "20.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Floor 7",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor 7" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 701",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 701" },
                    { AreaProperty, "30.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 8: No access rights defined for use case
    /// All instances exist but no properties are required
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) NoAccessRightsForUseCase()
    {
        var buildingId = "building-8";
        var floorId = "floor-8";
        var roomId = "room-8";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 8",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 8" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Floor 8",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor 8" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 801",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 801" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            }
        };

        // Empty access rights for this use case
        var accessRights = new List<AccessRight>();

        return (instances, accessRights, FacilityManagementUseCaseId);
    }

    /// <summary>
    /// Scenario 9: Complex graph with doors and windows
    /// Tests deeper graph structures with more element types
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) ComplexGraphWithOpenings()
    {
        var buildingId = "building-9";
        var floorId = "floor-9";
        var roomId = "room-9";
        var wallId = "wall-9";
        var doorId = "door-9";
        var windowId = "window-9";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 9",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 9" },
                    { DescriptionProperty, "Complete Building" },
                    { HeightProperty, "15.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Floor 9",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor 9" },
                    { HeightProperty, "3.2" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = wallId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 901",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 901" },
                    { AreaProperty, "25.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = wallId,
                Name = "External Wall",
                ClassificationId = WallClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "External Wall" },
                    { HeightProperty, "3.0" },
                    { WidthProperty, "0.3" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = wallId, PredicateUri = ContainsRelation },
                    new() { SubjectId = wallId, ObjectId = doorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = wallId, ObjectId = windowId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = doorId,
                Name = "Main Door",
                ClassificationId = DoorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Main Door" },
                    { HeightProperty, "2.1" },
                    { WidthProperty, "0.9" },
                    { FireRatingProperty, "EI30" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = wallId, ObjectId = doorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = windowId,
                Name = "Window 1",
                ClassificationId = WindowClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Window 1" },
                    { HeightProperty, "1.5" },
                    { WidthProperty, "1.2" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = wallId, ObjectId = windowId, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty),
            CreateAccessRight(ArchitecturalUseCaseId, WallClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, WallClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, WallClassId, WidthProperty),
            CreateAccessRight(ArchitecturalUseCaseId, DoorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, DoorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, DoorClassId, WidthProperty),
            CreateAccessRight(ArchitecturalUseCaseId, DoorClassId, FireRatingProperty),
            CreateAccessRight(ArchitecturalUseCaseId, WindowClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, WindowClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, WindowClassId, WidthProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 10: Graph with circular references
    /// Tests that circular relationships are handled correctly
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) CompleteGraphWithCircularReferences()
    {
        var buildingId = "building-10";
        var floorId = "floor-10";
        var roomId = "room-10";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 10",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 10" },
                    { DescriptionProperty, "Circular Refs Building" },
                    { HeightProperty, "20.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = buildingId, PredicateUri = "PartOf" } // Circular reference
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Floor 10",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor 10" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = buildingId, PredicateUri = "PartOf" }, // Circular reference
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation },
                    new() { SubjectId = roomId, ObjectId = floorId, PredicateUri = "LocatedIn" } // Circular reference
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 1001",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 1001" },
                    { AreaProperty, "40.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation },
                    new() { SubjectId = roomId, ObjectId = floorId, PredicateUri = "LocatedIn" } // Circular reference
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 11: Single instance complete (minimal graph)
    /// Only one instance that satisfies all use case requirements
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) SingleInstanceComplete()
    {
        var buildingId = "building-11";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Standalone Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Standalone Building" },
                    { DescriptionProperty, "Single instance" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(FacilityManagementUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(FacilityManagementUseCaseId, BuildingClassId, DescriptionProperty)
        };

        return (instances, accessRights, FacilityManagementUseCaseId);
    }

    /// <summary>
    /// Scenario 12: Partially complete - one complete, one incomplete subgraph
    /// Two buildings: one complete, one missing properties
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) MixedCompleteAndIncompleteSubgraphs()
    {
        var building1Id = "building-12-1";
        var floor1Id = "floor-12-1";
        var room1Id = "room-12-1";
        
        var building2Id = "building-12-2";
        var floor2Id = "floor-12-2";
        var room2Id = "room-12-2";

        var instances = new List<Instance>
        {
            // Complete subgraph
            new Instance
            {
                Id = building1Id,
                Name = "Complete Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Complete Building" },
                    { DescriptionProperty, "Fully specified" },
                    { HeightProperty, "25.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building1Id, ObjectId = floor1Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor1Id,
                Name = "Complete Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Complete Floor" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building1Id, ObjectId = floor1Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor1Id, ObjectId = room1Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room1Id,
                Name = "Complete Room",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Complete Room" },
                    { AreaProperty, "50.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor1Id, ObjectId = room1Id, PredicateUri = ContainsRelation }
                }
            },
            // Incomplete subgraph (missing room area)
            new Instance
            {
                Id = building2Id,
                Name = "Incomplete Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Incomplete Building" },
                    { DescriptionProperty, "Missing data" },
                    { HeightProperty, "20.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building2Id, ObjectId = floor2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floor2Id,
                Name = "Incomplete Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Incomplete Floor" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = building2Id, ObjectId = floor2Id, PredicateUri = ContainsRelation },
                    new() { SubjectId = floor2Id, ObjectId = room2Id, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = room2Id,
                Name = "Incomplete Room",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Incomplete Room" }
                    // Missing AreaProperty!
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floor2Id, ObjectId = room2Id, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 13: Instance with wrong classification not in use case
    /// Tests that irrelevant instances don't affect completeness
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) GraphWithIrrelevantInstances()
    {
        var buildingId = "building-13";
        var floorId = "floor-13";
        var roomId = "room-13";
        var doorId = "door-13"; // Not in use case requirements

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 13",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 13" },
                    { DescriptionProperty, "Building with extras" },
                    { HeightProperty, "22.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Floor 13",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Floor 13" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation },
                    new() { SubjectId = floorId, ObjectId = doorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = roomId,
                Name = "Room 1301",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Room 1301" },
                    { AreaProperty, "35.0" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = doorId,
                Name = "Extra Door",
                ClassificationId = DoorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Extra Door" },
                    { HeightProperty, "2.1" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = doorId, PredicateUri = ContainsRelation }
                }
            }
        };

        // Access rights don't include Door
        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, DescriptionProperty),
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, HeightProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, AreaProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 14: Empty graph
    /// No instances at all
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) EmptyGraph()
    {
        var instances = new List<Instance>();

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, FloorClassId, NameProperty),
            CreateAccessRight(ArchitecturalUseCaseId, RoomClassId, NameProperty)
        };

        return (instances, accessRights, ArchitecturalUseCaseId);
    }

    /// <summary>
    /// Scenario 15: Non-existent use case ID
    /// Valid instances but use case ID doesn't match any access rights
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) NonExistentUseCaseId()
    {
        var buildingId = "building-15";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Building 15",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, "Building 15" }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        var accessRights = new List<AccessRight>
        {
            CreateAccessRight(ArchitecturalUseCaseId, BuildingClassId, NameProperty)
        };

        // Return with a different use case ID that has no access rights
        return (instances, accessRights, "99999999-9999-9999-9999-999999999999");
    }

    /// <summary>
    /// Helper method to create access rights with consistent structure
    /// </summary>
    private static AccessRight CreateAccessRight(string useCaseId, string classificationId, string propertyName, PropertyRight right = PropertyRight.Read)
    {
        return new AccessRight
        {
            Id = Guid.NewGuid().ToString(),
            Name = propertyName,
            GuidelineClassificationId = classificationId,
            UserGroupId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UseCaseId = Guid.Parse(useCaseId),
            GuidlineClassificationPropertyId = $"{classificationId}/prop/{propertyName}",
            Right = right
        };
    }

    /// <summary>
    /// Helper method to create access rights for simple scenarios (Building, Floor, Room)
    /// </summary>
    private static List<AccessRight> CreateAccessRightsForSimpleScenario(string useCaseId, AccessRightVariant variant)
    {
        return variant switch
        {
            AccessRightVariant.AllRead => new List<AccessRight>
            {
                CreateAccessRight(useCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, BuildingClassId, DescriptionProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, FloorClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, RoomClassId, AreaProperty, PropertyRight.Read)
            },
            AccessRightVariant.AllNone => new List<AccessRight>
            {
                CreateAccessRight(useCaseId, BuildingClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, RoomClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, RoomClassId, AreaProperty, PropertyRight.None)
            },
            AccessRightVariant.Mixed => new List<AccessRight>
            {
                CreateAccessRight(useCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(useCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(useCaseId, RoomClassId, AreaProperty, PropertyRight.None)
            },
            _ => throw new ArgumentException($"Unknown variant: {variant}")
        };
    }
}
