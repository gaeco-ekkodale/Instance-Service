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
/// Provides scenarios for testing state transitions and concurrent operations
/// Tests how completeness changes as data is added/removed
/// </summary>
public static class StateTransitionScenarios
{
    // Classification IDs
    public const string BuildingClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcBuilding";
    public const string FloorClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcBuildingStorey";
    public const string RoomClassId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcSpace";

    // Property names
    public const string NameProperty = "Name";
    public const string DescriptionProperty = "Description";
    public const string HeightProperty = "Height";
    public const string AreaProperty = "Area";

    // Use Case IDs
    public const string StateUseCaseId = "51515151-5151-5151-5151-515151515151";

    // Relation label
    public const string ContainsRelation = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Contains";

    /// <summary>
    /// State Transition 1: Initially incomplete, becomes complete when property is added
    /// Provides both incomplete and complete versions
    /// </summary>
    public static class IncompleteToComplete
    {
        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InitialState()
        {
            var buildingId = "state-1-building";
            var floorId = "state-1-floor";

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
                        // Missing DescriptionProperty - INCOMPLETE
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                },
                new Instance
                {
                    Id = floorId,
                    Name = "Floor",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Floor" },
                        { HeightProperty, "3.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }

        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) AfterPropertyAdded()
        {
            var buildingId = "state-1-building";
            var floorId = "state-1-floor";

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
                        { DescriptionProperty, "Now complete!" } // Property added - NOW COMPLETE
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                },
                new Instance
                {
                    Id = floorId,
                    Name = "Floor",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Floor" },
                        { HeightProperty, "3.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }
    }

    /// <summary>
    /// State Transition 2: Initially incomplete, becomes complete when required instance is added
    /// </summary>
    public static class IncompleteToCompleteWithNewInstance
    {
        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InitialState()
        {
            var buildingId = "state-2-building";

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
                        { DescriptionProperty, "Building" }
                    },
                    Relations = new List<InstanceRelation>()
                }
                // Missing Floor instance - INCOMPLETE
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }

        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) AfterInstanceAdded()
        {
            var buildingId = "state-2-building";
            var floorId = "state-2-floor";

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
                        { DescriptionProperty, "Building" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                },
                new Instance
                {
                    Id = floorId,
                    Name = "Floor",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Floor" },
                        { HeightProperty, "3.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
                // Floor instance added - NOW COMPLETE
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }
    }

    /// <summary>
    /// State Transition 3: Initially complete, becomes incomplete when property is removed
    /// </summary>
    public static class CompleteToIncomplete
    {
        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InitialState()
        {
            var buildingId = "state-3-building";
            var floorId = "state-3-floor";

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
                        { DescriptionProperty, "Complete building" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                },
                new Instance
                {
                    Id = floorId,
                    Name = "Floor",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Floor" },
                        { HeightProperty, "3.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }

        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) AfterPropertyRemoved()
        {
            var buildingId = "state-3-building";
            var floorId = "state-3-floor";

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
                        // DescriptionProperty removed - NOW INCOMPLETE
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                },
                new Instance
                {
                    Id = floorId,
                    Name = "Floor",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Floor" },
                        { HeightProperty, "3.5" }
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }
    }

    /// <summary>
    /// State Transition 4: Access rights change - initially complete, becomes incomplete when requirements increase
    /// </summary>
    public static class AccessRightsIncrease
    {
        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InitialState()
        {
            var buildingId = "state-4-building";
            var floorId = "state-4-floor";

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
                        // Has NameProperty, which is all that's required
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
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
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            // Minimal access rights - COMPLETE with current data
            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }

        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) AfterAccessRightsAdded()
        {
            var buildingId = "state-4-building";
            var floorId = "state-4-floor";

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
                        // Still missing DescriptionProperty
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                },
                new Instance
                {
                    Id = floorId,
                    Name = "Floor",
                    ClassificationId = FloorClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Floor" }
                        // Still missing HeightProperty
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            // More access rights added - NOW INCOMPLETE because data doesn't match new requirements
            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty), // New requirement
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, HeightProperty) // New requirement
            };

            return (instances, accessRights, StateUseCaseId);
        }
    }

    /// <summary>
    /// State Transition 5: Access rights removed - incomplete becomes complete
    /// </summary>
    public static class AccessRightsDecrease
    {
        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) InitialState()
        {
            var buildingId = "state-5-building";
            var floorId = "state-5-floor";

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
                        // Missing DescriptionProperty
                    },
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
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
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            // INCOMPLETE - DescriptionProperty is required but missing
            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, BuildingClassId, DescriptionProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }

        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) AfterAccessRightsRemoved()
        {
            var buildingId = "state-5-building";
            var floorId = "state-5-floor";

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
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
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
                    Relations = new List<InstanceRelation>
                    {
                        new() { SubjectId = buildingId, ObjectId = floorId, PredicateUri = ContainsRelation }
                    }
                }
            };

            // DescriptionProperty requirement removed - NOW COMPLETE
            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty),
                CreateAccessRight(StateUseCaseId, FloorClassId, NameProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }
    }

    /// <summary>
    /// State Transition 6: Property value updated from empty to filled
    /// </summary>
    public static class PropertyValueUpdate
    {
        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) BeforeUpdate()
        {
            var buildingId = "state-6-building";

            var instances = new List<Instance>
            {
                new Instance
                {
                    Id = buildingId,
                    Name = "Building",
                    ClassificationId = BuildingClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "" } // Empty value - INCOMPLETE
                    },
                    Relations = new List<InstanceRelation>()
                }
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }

        public static (List<Instance> Instances, List<AccessRight> AccessRights, string UseCaseId) AfterUpdate()
        {
            var buildingId = "state-6-building";

            var instances = new List<Instance>
            {
                new Instance
                {
                    Id = buildingId,
                    Name = "Building",
                    ClassificationId = BuildingClassId,
                    Properties = new Dictionary<string, string>
                    {
                        { NameProperty, "Updated Building Name" } // Value filled - NOW COMPLETE
                    },
                    Relations = new List<InstanceRelation>()
                }
            };

            var accessRights = new List<AccessRight>
            {
                CreateAccessRight(StateUseCaseId, BuildingClassId, NameProperty)
            };

            return (instances, accessRights, StateUseCaseId);
        }
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
