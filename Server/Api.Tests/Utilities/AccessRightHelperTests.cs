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
using InstanceService.Api.Tests.Utilities.Faker;
using InstanceService.Api.Utilities;
using InstanceService.Models;
using InstanceService.Models.Enum;

namespace InstanceService.Api.Tests.Utilities
{
    /// <summary>
    /// Tests for the <see cref="AccessRightHelper"/> class.
    /// </summary>
    public class AccessRightHelperTests
    {
        private readonly AccessRightHelper _accessRightHelper;
        private readonly AccessRightFaker _accessRightFaker;

        public AccessRightHelperTests()
        {
            _accessRightHelper = new AccessRightHelper();
            _accessRightFaker = new AccessRightFaker();
        }

        #region HasFullControl

        [Fact]
        public void HasFullControl_ReturnsTrue_WhenAllRightsAreWrite()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid();
            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[0])
                .WithUseCaseId(useCaseId.ToString())
                .WithRight(PropertyRight.Write)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[1])
                .WithUseCaseId(useCaseId.ToString())
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId.ToString());

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasFullControl method when some rights are Read.
        /// </summary>
        public void HasFullControl_ReturnsFalse_WhenSomeRightsAreRead()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[0])
                .WithUseCaseId(useCaseId.ToString())
                .WithRight(PropertyRight.Read)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[1])
                .WithUseCaseId(useCaseId.ToString())
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId.ToString());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasFullControl method when all rights are Read.
        /// </summary>
        public void HasFullControl_ReturnsFalse_WhenAllRightsAreRead()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[0])
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[1])
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasFullControl method when no rights are present for the classification.
        /// </summary>
        public void HasFullControl_ReturnsFalse_WhenNoRightsForClassification()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>();

            // Act
            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasFullControl method when the use case ID does not match.
        /// </summary>
        public void HasFullControl_ReturnsFalse_WhenUseCaseIdDoesNotMatch()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds[0])
                .WithUseCaseId(Guid.NewGuid().ToString())
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasFullControl method when no group has rights for the classification.
        /// </summary>
        public void HasFullControl_ReturnsFalse_WhenNoGroupHasRightsForClassification()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(Guid.NewGuid().ToString())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasFullControl method when no group exists.
        /// </summary>
        public void HasFullControl_ReturnsFalse_WhenGroupIdsIsEmpty()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string>();
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(Guid.NewGuid().ToString()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Write).Generate()
        };

            var result = _accessRightHelper.HasFullControl(classificationId, groupIds, accessRights, useCaseId);

            result.Should().BeFalse();
        }

        #endregion HasFullControl

        #region HasWrite

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when at least one Write right is present.
        /// </summary>
        public void HasWrite_ReturnsTrue_WhenAtLeastOneWriteRightIsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.Last())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when no Write rights are present.
        /// </summary>
        [Fact]
        public void HasWrite_ReturnsFalse_WhenNoWriteRightsArePresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.Last())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when no access rights are provided.
        /// </summary>
        public void HasWrite_ReturnsFalse_WhenNoAccessRightsProvided()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>(); // No access rights provided

            // Act
            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when no rights match the classification ID.
        /// </summary>
        public void HasWrite_ReturnsFalse_WhenNoRightsMatchClassificationId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var nonMatchingClassificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWall";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(nonMatchingClassificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when no rights match the use case ID.
        /// </summary>
        public void HasWrite_ReturnsFalse_WhenNoRightsMatchUseCaseId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var nonMatchingUseCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(nonMatchingUseCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when no rights match the group IDs.
        /// </summary>
        public void HasWrite_ReturnsFalse_WhenNoRightsMatchGroupIds()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var nonMatchingGroupId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(nonMatchingGroupId)
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasWrite method when no group exists.
        /// </summary>
        public void HasWrite_ReturnsFalse_WhenGroupIdsIsEmpty()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string>();
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(Guid.NewGuid().ToString()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Write).Generate()
        };

            var result = _accessRightHelper.HasWrite(classificationId, groupIds, accessRights, useCaseId);

            result.Should().BeFalse();
        }

        #endregion HasWrite

        #region HasReadOnly

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when no access rights are provided.
        /// </summary>
        public void HasReadOnly_ReturnsFalse_WhenNoAccessRightsProvided()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>();

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when only Write rights are present.
        /// </summary>
        public void HasReadOnly_ReturnsFalse_WhenOnlyWriteRightsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when Read rights are present.
        /// </summary>
        public void HasReadOnly_ReturnsTrue_WhenReadRightsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when no rights match the classification ID.
        /// </summary>
        public void HasReadOnly_ReturnsFalse_WhenNoRightsMatchClassificationId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var nonMatchingClassificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWall";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(nonMatchingClassificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when no rights match the use case ID.
        /// </summary>
        public void HasReadOnly_ReturnsFalse_WhenNoRightsMatchUseCaseId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var nonMatchingUseCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(nonMatchingUseCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when no rights match the group IDs.
        /// </summary>
        public void HasReadOnly_ReturnsFalse_WhenNoRightsMatchGroupIds()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var nonMatchingGroupId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(nonMatchingGroupId)
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when both Read and Write rights are present.
        /// </summary>
        public void HasReadOnly_ReturnsTrue_WhenReadAndWriteRightsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasReadOnly method when no group exists.
        /// </summary>
        public void HasReadOnly_ReturnsFalse_WhenGroupIdsIsEmpty()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string>();
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(Guid.NewGuid().ToString()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Write).Generate()
        };

            var result = _accessRightHelper.HasReadOnly(classificationId, groupIds, accessRights, useCaseId);

            result.Should().BeFalse();
        }

        #endregion HasReadOnly

        #region HasNone

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasNone method when no access rights are provided.
        /// </summary>
        public void HasNone_ReturnsTrue_WhenNoAccessRightsProvided()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>();

            // Act
            var result = _accessRightHelper.HasNone(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasNone method when Read rights are present.
        /// </summary>
        public void HasNone_ReturnsFalse_WhenReadRightsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasNone(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasNone method when Write rights are present.
        /// </summary>
        public void HasNone_ReturnsFalse_WhenWriteRightsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasNone(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasNone method when both Read and Write rights are present.
        /// </summary>
        public void HasNone_ReturnsFalse_WhenReadAndWriteRightsPresent()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Read)
                .Generate(),
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasNone(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasNone method when no rights match the classification ID.
        /// </summary>
        public void HasNone_ReturnsTrue_WhenNoRightsMatchClassificationId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var nonMatchingClassificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWall";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(nonMatchingClassificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.None)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.HasNone(classificationId, groupIds, accessRights, useCaseId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.HasNone method when no group exists.
        /// </summary>
        public void HasNone_ReturnsTrue_WhenGroupIdsIsEmpty()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string>();
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(Guid.NewGuid().ToString()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Write).Generate()
        };

            var result = _accessRightHelper.HasNone(classificationId, groupIds, accessRights, useCaseId);

            result.Should().BeTrue();
        }

        #endregion HasNone

        #region GetFilteredAccessRights

        [Fact]
        public void GetFilteredAccessRights_ReturnsOnlyMatchingAccessRights()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(groupIds.First()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Read).Generate(),
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(groupIds.Last()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Write).Generate(),
            _accessRightFaker.WithClassificationId("https://example.com/otherClassification").WithUserGroupId(groupIds.First()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Read).Generate()
        };

            var filteredRights = _accessRightHelper.GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);

            filteredRights.Should().HaveCount(2);
            filteredRights.Should().Contain(x => x.UserGroupId.ToString() == groupIds.First());
            filteredRights.Should().Contain(x => x.UserGroupId.ToString() == groupIds.Last());
        }

        [Fact]
        public void GetFilteredAccessRights_ReturnsEmpty_WhenNoMatchingAccessRights()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId("https://example.com/otherClassification").WithUserGroupId(groupIds.First()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Read).Generate()
        };

            var filteredRights = _accessRightHelper.GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);

            filteredRights.Should().BeEmpty();
        }

        [Fact]
        public void GetFilteredAccessRights_ReturnsEmpty_WhenNoRightsProvided()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>();

            var filteredRights = _accessRightHelper.GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);

            filteredRights.Should().BeEmpty();
        }

        [Fact]
        public void GetFilteredAccessRights_ReturnsEmpty_WhenGroupIdsIsEmpty()
        {
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string>();
            var useCaseId = Guid.NewGuid().ToString();

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker.WithClassificationId(classificationId).WithUserGroupId(Guid.NewGuid().ToString()).WithUseCaseId(useCaseId).WithRight(PropertyRight.Read).Generate()
        };

            var filteredRights = _accessRightHelper.GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);

            filteredRights.Should().BeEmpty();
        }

        #endregion GetFilteredAccessRights

        #region CanUpdate

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.CanUpdate method when no access rights are provided.
        /// </summary>
        public void CanUpdate_ReturnsFalse_WhenNoAccessRightsProvided()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var propertyKey = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Country";

            var accessRights = new List<AccessRight>();

            // Act
            var result = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, propertyKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.CanUpdate method when only Write rights for a different property are present.
        /// </summary>
        public void CanUpdate_ReturnsFalse_WhenOnlyWriteRightsForDifferentProperty()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var propertyKey = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Country";

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .WithPropertyId("https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Region")
                .Generate()
        };

            // Act
            var result = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, propertyKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.CanUpdate method when Write rights for the correct property are present.
        /// </summary>
        public void CanUpdate_ReturnsTrue_WhenWriteRightsForCorrectProperty()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var propertyKey = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Country";

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .WithPropertyId(propertyKey)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, propertyKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.CanUpdate method when no rights match the classification ID.
        /// </summary>
        public void CanUpdate_ReturnsFalse_WhenNoRightsMatchClassificationId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var nonMatchingClassificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcWall";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var propertyKey = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Country";

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(nonMatchingClassificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .WithPropertyId(propertyKey)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, propertyKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.CanUpdate method when no rights match the use case ID.
        /// </summary>
        public void CanUpdate_ReturnsFalse_WhenNoRightsMatchUseCaseId()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var nonMatchingUseCaseId = Guid.NewGuid().ToString();
            var propertyKey = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Country";

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(groupIds.First())
                .WithUseCaseId(nonMatchingUseCaseId)
                .WithRight(PropertyRight.Write)
                .WithPropertyId(propertyKey)
                .Generate()
        };

            // Act
            var result = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, propertyKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        /// <summary>
        /// Tests the AccessRightHelper.CanUpdate method when no rights match the group IDs.
        /// </summary>
        public void CanUpdate_ReturnsFalse_WhenNoRightsMatchGroupIds()
        {
            // Arrange
            var classificationId = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/class/IfcActor";
            var groupIds = new List<string> { Guid.NewGuid().ToString() };
            var useCaseId = Guid.NewGuid().ToString();
            var nonMatchingGroupId = Guid.NewGuid().ToString();
            var propertyKey = "https://identifier.buildingsmart.org/uri/buildingsmart/ifc/4.3/prop/Country";

            var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUserGroupId(nonMatchingGroupId)
                .WithUseCaseId(useCaseId)
                .WithRight(PropertyRight.Write)
                .WithPropertyId(propertyKey)
        };

            // Act
            var result = _accessRightHelper.CanUpdate(classificationId, groupIds, accessRights, useCaseId, propertyKey);

            // Assert
            result.Should().BeFalse();
        }

        #endregion CanUpdate
    }
}