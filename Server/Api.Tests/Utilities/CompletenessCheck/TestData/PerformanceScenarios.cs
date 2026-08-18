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
/// Provides performance and stress test scenarios for completeness check testing
/// Tests system behavior under high load and complex conditions
/// Each scenario provides three AccessRight variants: AllRead, AllNone, and Mixed
/// </summary>
public static class PerformanceScenarios
{
    // Classification IDs
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

    // Use Case IDs
    public const string PerformanceUseCaseId = "f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1";

    // Relation label
    public const string ContainsRelation = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Contains";

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
    /// Performance 1: Large complete graph (realistic building model)
    /// 1 building, 10 floors, 20 rooms per floor = 211 instances total
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) LargeCompleteGraph(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var instances = new List<Instance>();
        var buildingId = "perf-building-1";
        var floorCount = 10;
        var roomsPerFloor = 20;

        // Create building
        var building = new Instance
        {
            Id = buildingId,
            Name = "Large Building",
            ClassificationId = BuildingClassId,
            Properties = new Dictionary<string, string>
            {
                { NameProperty, "Large Building" },
                { DescriptionProperty, "Performance test building" },
                { HeightProperty, "35.0" }
            },
            Relations = new List<InstanceRelation>()
        };
        instances.Add(building);

        // Create floors and rooms
        for (int f = 0; f < floorCount; f++)
        {
            var floorId = $"perf-floor-{f}";
            var floor = new Instance
            {
                Id = floorId,
                Name = $"Floor {f}",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, $"Floor {f}" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            };
            
            building.Relations.Add(new InstanceRelation
            {
                SubjectId = buildingId,
                ObjectId = floorId,
                PredicateUri = ContainsRelation
            });
            
            instances.Add(floor);

            // Create rooms for this floor
            for (int r = 0; r < roomsPerFloor; r++)
            {
                var roomId = $"perf-room-{f}-{r}";
                var room = new Instance
                {
                    Id = roomId,
                    Name = $"Room {f}{r:D2}",
                    ClassificationId = RoomClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, $"Room {f}{r:D2}" },
                        { AreaProperty, $"{25 + r}.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                    }
                };
                
                floor.Relations.Add(new InstanceRelation
                {
                    SubjectId = floorId,
                    ObjectId = roomId,
                    PredicateUri = ContainsRelation
                });
                
                instances.Add(room);
            }
        }

        var accessRights = variant switch
        {
            AccessRightVariant.AllRead => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.Read)
            },
            AccessRightVariant.AllNone => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.None)
            },
            AccessRightVariant.Mixed => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.None)
            },
            _ => throw new ArgumentException($"Unknown variant: {variant}")
        };

        return (instances, accessRights, PerformanceUseCaseId);
    }

    /// <summary>
    /// Performance 2: Multiple large buildings (tests CheckAndSendForMultipleInstancesAsync)
    /// 5 buildings, each with 5 floors and 10 rooms = 305 instances total
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) MultipleLargeBuildings(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var instances = new List<Instance>();
        var buildingCount = 5;
        var floorsPerBuilding = 5;
        var roomsPerFloor = 10;

        for (int b = 0; b < buildingCount; b++)
        {
            var buildingId = $"perf-multi-building-{b}";
            var building = new Instance
            {
                Id = buildingId,
                Name = $"Building {b}",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, $"Building {b}" },
                    { DescriptionProperty, $"Multi-building test {b}" },
                    { HeightProperty, "20.0" }
                },
                Relations = new List<InstanceRelation>()
            };
            instances.Add(building);

            for (int f = 0; f < floorsPerBuilding; f++)
            {
                var floorId = $"perf-multi-floor-{b}-{f}";
                var floor = new Instance
                {
                    Id = floorId,
                    Name = $"Building {b} Floor {f}",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, $"Building {b} Floor {f}" },
                        { HeightProperty, "3.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                };
                
                building.Relations.Add(new InstanceRelation
                {
                    SubjectId = buildingId,
                    ObjectId = floorId,
                    PredicateUri = ContainsRelation
                });
                
                instances.Add(floor);

                for (int r = 0; r < roomsPerFloor; r++)
                {
                    var roomId = $"perf-multi-room-{b}-{f}-{r}";
                    var room = new Instance
                    {
                        Id = roomId,
                        Name = $"B{b}F{f}R{r}",
                        ClassificationId = RoomClassId,
                        Properties = new Dictionary<string, string>
                        {
                            { NameProperty, $"B{b}F{f}R{r}" },
                            { AreaProperty, "30.0" }
                        },
                        Relations = new List<InstanceRelation>
                        {
                            new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                        }
                    };
                    
                    floor.Relations.Add(new InstanceRelation
                    {
                        SubjectId = floorId,
                        ObjectId = roomId,
                        PredicateUri = ContainsRelation
                    });
                    
                    instances.Add(room);
                }
            }
        }

        var accessRights = variant switch
        {
            AccessRightVariant.AllRead => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.Read)
            },
            AccessRightVariant.AllNone => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.None)
            },
            AccessRightVariant.Mixed => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.None)
            },
            _ => throw new ArgumentException($"Unknown variant: {variant}")
        };

        return (instances, accessRights, PerformanceUseCaseId);
    }

    /// <summary>
    /// Performance 3: Highly interconnected graph (mesh topology)
    /// Tests performance with many cross-references between nodes
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) HighlyInterconnectedGraph(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var instances = new List<Instance>();
        var nodeCount = 20;
        var connectionsPerNode = 5;

        // Create all instances first
        for (int i = 0; i < nodeCount; i++)
        {
            var instanceId = $"perf-mesh-{i}";
            var classId = i % 3 == 0 ? BuildingClassId : (i % 3 == 1 ? FloorClassId : RoomClassId);
            
            instances.Add(new Instance
            {
                Id = instanceId,
                Name = $"Node {i}",
                ClassificationId = classId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, $"Node {i}" }
                },
                Relations = new List<InstanceRelation>()
            });
        }

        // Create interconnections
        for (int i = 0; i < nodeCount; i++)
        {
            var sourceInstance = instances[i];
            for (int c = 1; c <= connectionsPerNode; c++)
            {
                var targetIndex = (i + c) % nodeCount;
                var targetId = instances[targetIndex].Id;
                
                sourceInstance.Relations.Add(new InstanceRelation
                {
                    SubjectId = sourceInstance.Id,
                    ObjectId = targetId,
                    PredicateUri = $"Connection{c}"
                });
            }
        }

        var accessRights = variant switch
        {
            AccessRightVariant.AllRead => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read)
            },
            AccessRightVariant.AllNone => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.None)
            },
            AccessRightVariant.Mixed => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read)
            },
            _ => throw new ArgumentException($"Unknown variant: {variant}")
        };

        return (instances, accessRights, PerformanceUseCaseId);
    }

    /// <summary>
    /// Performance 4: Many properties per instance
    /// Tests handling of instances with large property dictionaries
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) ManyPropertiesPerInstance(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var buildingId = "perf-many-props-building";
        var floorId = "perf-many-props-floor";
        var propertyCount = 50;

        var building = new Instance
        {
            Id = buildingId,
            Name = "Building with many properties",
            ClassificationId = BuildingClassId,
            Properties = new Dictionary<string, string>(),
            Relations = new List<InstanceRelation>
            {
                new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
            }
        };

        // Add many properties
        for (int i = 0; i < propertyCount; i++)
        {
            building.Properties[$"Property{i}"] = $"Value{i}";
        }

        var floor = new Instance
        {
            Id = floorId,
            Name = "Floor with many properties",
            ClassificationId = FloorClassId,
            Properties = new Dictionary<string, string>(),
            Relations = new List<InstanceRelation>
            {
                new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
            }
        };

        for (int i = 0; i < propertyCount; i++)
        {
            floor.Properties[$"FloorProperty{i}"] = $"FloorValue{i}";
        }

        var instances = new List<Instance> { building, floor };

        // Create access rights for all properties
        var accessRights = new List<AccessRight>();
        for (int i = 0; i < propertyCount; i++)
        {
            var buildingRight = variant switch
            {
                AccessRightVariant.AllRead => PropertyRight.Read,
                AccessRightVariant.AllNone => PropertyRight.None,
                AccessRightVariant.Mixed => i % 2 == 0 ? PropertyRight.Read : PropertyRight.None,
                _ => throw new ArgumentException($"Unknown variant: {variant}")
            };
            
            var floorRight = variant switch
            {
                AccessRightVariant.AllRead => PropertyRight.Read,
                AccessRightVariant.AllNone => PropertyRight.None,
                AccessRightVariant.Mixed => i % 3 == 0 ? PropertyRight.Read : PropertyRight.None,
                _ => throw new ArgumentException($"Unknown variant: {variant}")
            };
            
            accessRights.Add(CreateAccessRight(PerformanceUseCaseId, BuildingClassId, $"Property{i}", buildingRight));
            accessRights.Add(CreateAccessRight(PerformanceUseCaseId, FloorClassId, $"FloorProperty{i}", floorRight));
        }

        return (instances, accessRights, PerformanceUseCaseId);
    }

    /// <summary>
    /// Performance 5: Many use cases on same graph
    /// Tests handling of multiple use cases with overlapping requirements
    /// </summary>
    public static (List<Instance> Instances, Dictionary<string, List<AccessRight>> AccessRightsByUseCase) ManyUseCasesSameGraph(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var buildingId = "perf-multi-usecase-building";
        var floorId = "perf-multi-usecase-floor";
        var roomId = "perf-multi-usecase-room";

        var instances = new List<Instance>
        {
            new Instance
            {
                Id = buildingId,
                Name = "Multi-usecase Building",
                ClassificationId = BuildingClassId,
                Properties = new Dictionary<string, string>
                {
                    { "Prop1", "Value1" },
                    { "Prop2", "Value2" },
                    { "Prop3", "Value3" },
                    { "Prop4", "Value4" },
                    { "Prop5", "Value5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            },
            new Instance
            {
                Id = floorId,
                Name = "Multi-usecase Floor",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { "Prop1", "Value1" },
                    { "Prop2", "Value2" },
                    { "Prop3", "Value3" }
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
                Name = "Multi-usecase Room",
                ClassificationId = RoomClassId,
                Properties = new Dictionary<string, string>
                {
                    { "Prop1", "Value1" },
                    { "Prop2", "Value2" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                }
            }
        };

        var accessRightsByUseCase = new Dictionary<string, List<AccessRight>>();
        
        // Create 10 different use cases
        for (int uc = 0; uc < 10; uc++)
        {
            var useCaseId = $"f{uc}f{uc}f{uc}f{uc}-f{uc}f{uc}-f{uc}f{uc}-f{uc}f{uc}-f{uc}f{uc}f{uc}f{uc}f{uc}f{uc}";
            var accessRights = new List<AccessRight>();
            
            // Each use case requires different properties
            var propsForUseCase = (uc % 5) + 1;
            for (int p = 1; p <= propsForUseCase; p++)
            {
                var right = variant switch
                {
                    AccessRightVariant.AllRead => PropertyRight.Read,
                    AccessRightVariant.AllNone => PropertyRight.None,
                    AccessRightVariant.Mixed => p % 2 == 1 ? PropertyRight.Read : PropertyRight.None,
                    _ => throw new ArgumentException($"Unknown variant: {variant}")
                };
                
                accessRights.Add(CreateAccessRight(useCaseId, BuildingClassId, $"Prop{p}", right));
                if (p <= 3)
                    accessRights.Add(CreateAccessRight(useCaseId, FloorClassId, $"Prop{p}", right));
                if (p <= 2)
                    accessRights.Add(CreateAccessRight(useCaseId, RoomClassId, $"Prop{p}", right));
            }
            
            accessRightsByUseCase[useCaseId] = accessRights;
        }

        return (instances, accessRightsByUseCase);
    }

    /// <summary>
    /// Performance 6: Complex building with all element types
    /// Realistic scenario with Building > Floor > Room > Wall > Door/Window
    /// </summary>
    public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) ComplexRealisticBuilding(AccessRightVariant variant = AccessRightVariant.AllRead)
    {
        var instances = new List<Instance>();
        var buildingId = "perf-complex-building";
        var floorCount = 3;
        var roomsPerFloor = 5;
        var wallsPerRoom = 4;
        var openingsPerWall = 2;

        // Create building
        var building = new Instance
        {
            Id = buildingId,
            Name = "Complex Realistic Building",
            ClassificationId = BuildingClassId,
            Properties = new Dictionary<string, string>
            {
                { NameProperty, "Complex Realistic Building" },
                { DescriptionProperty, "Full detail building" },
                { HeightProperty, "12.0" }
            },
            Relations = new List<InstanceRelation>()
        };
        instances.Add(building);

        for (int f = 0; f < floorCount; f++)
        {
            var floorId = $"perf-complex-floor-{f}";
            var floor = new Instance
            {
                Id = floorId,
                Name = $"Floor {f}",
                ClassificationId = FloorClassId,
                Properties = new Dictionary<string, string>
                {
                    { NameProperty, $"Floor {f}" },
                    { HeightProperty, "3.5" }
                },
                Relations = new List<InstanceRelation>
                {
                    new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                }
            };
            building.Relations.Add(new InstanceRelation { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation });
            instances.Add(floor);

            for (int r = 0; r < roomsPerFloor; r++)
            {
                var roomId = $"perf-complex-room-{f}-{r}";
                var room = new Instance
                {
                    Id = roomId,
                    Name = $"Room {f}{r}",
                    ClassificationId = RoomClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, $"Room {f}{r}" },
                        { AreaProperty, "25.0" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation }
                    }
                };
                floor.Relations.Add(new InstanceRelation { SubjectId = floorId, ObjectId = roomId, PredicateUri = ContainsRelation });
                instances.Add(room);

                for (int w = 0; w < wallsPerRoom; w++)
                {
                    var wallId = $"perf-complex-wall-{f}-{r}-{w}";
                    var wall = new Instance
                    {
                        Id = wallId,
                        Name = $"Wall {f}{r}{w}",
                        ClassificationId = WallClassId,
                        Properties = new Dictionary<string, string>
                        {
                            { NameProperty, $"Wall {f}{r}{w}" },
                            { HeightProperty, "3.0" },
                            { WidthProperty, "0.25" }
                        },
                        Relations = new List<InstanceRelation>
                        {
                            new() { SubjectId = roomId, ObjectId = wallId, PredicateUri = ContainsRelation }
                        }
                    };
                    room.Relations.Add(new InstanceRelation { SubjectId = roomId, ObjectId = wallId, PredicateUri = ContainsRelation });
                    instances.Add(wall);

                    // Add door and window to each wall
                    for (int o = 0; o < openingsPerWall; o++)
                    {
                        var isDoor = o % 2 == 0;
                        var openingId = $"perf-complex-opening-{f}-{r}-{w}-{o}";
                        var opening = new Instance
                        {
                            Id = openingId,
                            Name = $"{(isDoor ? "Door" : "Window")} {f}{r}{w}{o}",
                            ClassificationId = isDoor ? DoorClassId : WindowClassId,
                            Properties = new Dictionary<string, string>
                            {
                                { NameProperty, $"{(isDoor ? "Door" : "Window")} {f}{r}{w}{o}" },
                                { HeightProperty, isDoor ? "2.1" : "1.5" },
                                { WidthProperty, isDoor ? "0.9" : "1.2" }
                            },
                            Relations = new List<InstanceRelation>
                            {
                                new() { SubjectId = wallId, ObjectId = openingId, PredicateUri = ContainsRelation }
                            }
                        };
                        wall.Relations.Add(new InstanceRelation { SubjectId = wallId, ObjectId = openingId, PredicateUri = ContainsRelation });
                        instances.Add(opening);
                    }
                }
            }
        }

        var accessRights = variant switch
        {
            AccessRightVariant.AllRead => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, WidthProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, WidthProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, WidthProperty, PropertyRight.Read)
            },
            AccessRightVariant.AllNone => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, WidthProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, WidthProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, WidthProperty, PropertyRight.None)
            },
            AccessRightVariant.Mixed => new List<AccessRight>
            {
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, DescriptionProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, BuildingClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, FloorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, RoomClassId, AreaProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WallClassId, WidthProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, NameProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, HeightProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, DoorClassId, WidthProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, NameProperty, PropertyRight.None),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, HeightProperty, PropertyRight.Read),
                CreateAccessRight(PerformanceUseCaseId, WindowClassId, WidthProperty, PropertyRight.None)
            },
            _ => throw new ArgumentException($"Unknown variant: {variant}")
        };

        return (instances, accessRights, PerformanceUseCaseId);
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
}
