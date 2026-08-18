// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Model;
using InstanceService.Models;
using InstanceService.Models.Enum;
using VDS.RDF.Configuration.Permissions;

namespace InstanceService.Api.Utilities
{
    /// <summary>
    /// Provides a collection of helper methods to retrieve access rights filtered via classification, usergroups and use case and based on that determine accessibility for operations.
    /// </summary>
    public interface IAccessRightHelper
    {
        /// <summary>
        /// Filters access rights based on the provided criteria.
        /// </summary>
        IEnumerable<AccessRight> GetFilteredAccessRights(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Check if the user has Full Control access.
        /// </summary>
        bool HasFullControl(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Check if the user has Write access.
        /// </summary>
        bool HasWrite(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Check if the user has Read Only access.
        /// </summary>
        bool HasReadOnly(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Check if the user has None access.
        /// </summary>
        bool HasNone(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether a user is able to create an instance.
        /// </summary>
        /// <param name="classificationId">The id of the classification of the instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        bool CanCreate(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether a user is able to delete an instance.
        /// Requires at least one Write right on the classification. Whether read-only properties
        /// are empty must be verified separately by the caller.
        /// </summary>
        /// <param name="classificationId">The id of the classification of the instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        bool CanDelete(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether a user is able to delete relations between instances.
        /// </summary>
        /// <param name="classificationId1">The id of the classification of the subject instance.</param>
        /// <param name="classificationId2">The id of the classification of the object instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        bool CanDeleteRelations(string classificationId1, string classificationId2, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether a user is able to see instances.
        /// </summary>
        /// <param name="classificationId">The id of the classification of the instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        bool CanGet(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether a user is able to retrieve metadata of an instance.
        /// </summary>
        /// <param name="classificationId">The id of the classification of the instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        bool CanGetMetadata(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether a user is able to update a property of an instance.
        /// </summary>
        /// <param name="classificationId">The id of the classification of the instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        /// <param name="propertyKey">The key of the property to check accessibiltiy for.</param>
        bool CanUpdate(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId, string propertyKey);

        /// <summary>
        /// Helper method to determine whether a user is able to create relations between instances.
        /// </summary>
        /// <param name="classificationId1">The id of the classification of the subject instance.</param>
        /// <param name="classificationId2">The id of the classification of the object instance.</param>
        /// <param name="groupIds">The user groups to check accessibiltiy for.</param>
        /// <param name="accessRights">All access rights available.</param>
        /// <param name="useCaseId">The use case to check accessibility for.</param>
        bool CanCreateRelations(string classificationId1, string classificationId2, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether or not the user has access to the requested data.
        /// </summary>
        /// <param name="classificationId">The requested classificationId.</param>
        /// <param name="groupIds">The user groups the user belongs to.</param>
        /// <param name="accessRights">The access rights.</param>
        /// <param name="useCaseId">The Id of the current use case.</param>
        bool HasAccessibility(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to determine whether or not the user has access to the requested data.
        /// </summary>
        /// <param name="classificationId">The requested classificationId.</param>
        /// <param name="groupIds">The user groups the user belongs to.</param>
        /// <param name="accessRights">The access rights.</param>
        /// <param name="useCaseId">The Id of the current use case.</param>
        bool HasWriteAccessibility(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Helper method to return the user with the highest right for an accessright
        /// </summary>
        /// <param name="classificationId">The requested classificationId.</param>
        /// <param name="propertyId">The access rights.</param>
        /// <param name="groupIds">The user groups the user belongs to.</param>
        /// <param name="accessRights">All access rights.</param>
        /// <param name="useCaseId">The Id of the current use case.</param>
        string? GetUserGroupWithMostRights(string classificationId, string propertyId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId);

        /// <summary>
        /// Filters out one specific AccessRight.
        /// </summary>
        /// <param name="property">The classifications property to be filtered by.</param>
        /// <param name="accessRights">The access rights applicable to the property.</param>
        /// <param name="GuidelineClassificationId">The GuidelineClassificationId to be filtered by.</param>
        /// <param name="UseCaseId">The UseCaseId to be filtered by.</param>
        /// <param name="UserGroups">The User Group to be filtered by.</param>
        /// <returns>The filtered Property.</returns>
        public AccessRight FilterSingleAccessRight(IClassificationProperty property, IEnumerable<AccessRight> accessRights, string GuidelineClassificationId, string UseCaseId, List<string> UserGroups);
    }

    /// <summary>
    /// Provides a collection of helper methods to retrieve access rights filtered via classification, usergroups and use case and based on that determine accessibility for operations.
    /// </summary>
    public class AccessRightHelper : IAccessRightHelper
    {
        /// <inheritdoc />
        public IEnumerable<AccessRight> GetFilteredAccessRights(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return accessRights.Where(accessRight =>
                accessRight.GuidelineClassificationId == classificationId &&
                groupIds.Contains(accessRight.UserGroupId.ToString()) &&
                accessRight.UseCaseId.ToString() == useCaseId);
        }

        /// <inheritdoc />
        public bool HasFullControl(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            var filteredRights = GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);

            if (!filteredRights.Any())
            {
                return false;
            }

            return filteredRights.All(accessRight => accessRight.Right == PropertyRight.Write);
        }

        /// <inheritdoc />
        public bool HasWrite(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            var filteredRights = GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);
            return filteredRights.Any(accessRight => accessRight.Right == PropertyRight.Write);
        }

        /// <inheritdoc />
        public bool HasReadOnly(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            var filteredRights = GetFilteredAccessRights(classificationId, groupIds, accessRights, useCaseId);
            return filteredRights.Any(accessRight => accessRight.Right == PropertyRight.Read);
        }

        /// <inheritdoc />
        public bool HasNone(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return !HasReadOnly(classificationId, groupIds, accessRights, useCaseId) &&
                   !HasWrite(classificationId, groupIds, accessRights, useCaseId) &&
                   !HasFullControl(classificationId, groupIds, accessRights, useCaseId);
        }

        /// <inheritdoc />
        public bool CanCreate(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return HasWrite(classificationId, groupIds, accessRights, useCaseId) || HasFullControl(classificationId, groupIds, accessRights, useCaseId);
        }

        /// <inheritdoc />
        public bool CanDelete(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return HasWrite(classificationId, groupIds, accessRights, useCaseId) || HasFullControl(classificationId, groupIds, accessRights, useCaseId);
        }

        /// <inheritdoc />
        public bool CanDeleteRelations(string classificationId1, string classificationId2, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return HasFullControl(classificationId1, groupIds, accessRights, useCaseId) &&
                   HasFullControl(classificationId2, groupIds, accessRights, useCaseId);
        }

        /// <inheritdoc />
        public bool CanGet(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return true;
        }

        /// <inheritdoc />
        public bool CanGetMetadata(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return !HasNone(classificationId, groupIds, accessRights, useCaseId);
        }

        /// <inheritdoc />
        public bool CanUpdate(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId, string propertyKey)
        {
            var propertyAccessRights = accessRights.
                Where(right => right.GuidelineClassificationId == classificationId
                && right.GuidlineClassificationPropertyId == propertyKey
                && groupIds.Contains(right.UserGroupId.ToString())
                && right.UseCaseId.ToString() == useCaseId);

            return HasWrite(classificationId, groupIds, propertyAccessRights, useCaseId) || HasFullControl(classificationId, groupIds, propertyAccessRights, useCaseId);
        }

        /// <inheritdoc />
        public bool CanCreateRelations(string classificationId1, string classificationId2, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return (HasWrite(classificationId1, groupIds, accessRights, useCaseId) &&
                   HasWrite(classificationId2, groupIds, accessRights, useCaseId)) || (HasFullControl(classificationId1, groupIds, accessRights, useCaseId) &&
                   HasFullControl(classificationId2, groupIds, accessRights, useCaseId));
        }

        /// <inheritdoc />
        public bool HasWriteAccessibility(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return groupIds.Any(groupId => accessRights.Any(accessRight =>
                accessRight.GuidelineClassificationId == classificationId &&
                accessRight.UserGroupId.ToString() == groupId &&
                accessRight.UseCaseId.ToString() == useCaseId &&
                (accessRight.Right == PropertyRight.Write)));
        }

        /// <inheritdoc />
        public bool HasAccessibility(string classificationId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            return groupIds.Any(groupId => accessRights.Any(accessRight =>
                accessRight.GuidelineClassificationId == classificationId &&
                accessRight.UserGroupId.ToString() == groupId &&
                accessRight.UseCaseId.ToString() == useCaseId &&
                (accessRight.Right == PropertyRight.Write || accessRight.Right == PropertyRight.Read)));
        }

        /// <inheritdoc />
        public string? GetUserGroupWithMostRights(string classificationId, string propertyId, List<string> groupIds, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            var filtered = accessRights.Where(accessRight =>
                accessRight.GuidlineClassificationPropertyId == propertyId &&
                accessRight.GuidelineClassificationId == classificationId &&
                groupIds.Contains(accessRight.UserGroupId.ToString()) &&
                accessRight.UseCaseId.ToString() == useCaseId);

            var mostRights = filtered.FirstOrDefault(ar => ar.Right == PropertyRight.Write) ?? 
                                filtered.FirstOrDefault(ar => ar.Right == PropertyRight.Read);

            return mostRights?.UserGroupId.ToString();
        }

        /// <inheritdoc />
        public AccessRight? FilterSingleAccessRight(IClassificationProperty property, IEnumerable<AccessRight> accessRights, string GuidelineClassificationId, string UseCaseId, List<string> UserGroups)
        {
            var propertyId = property?.PropertyAssignment?.Property?.Identifier;

            return accessRights
                .Where(ar => ar.GuidlineClassificationPropertyId == propertyId
                    && ar.GuidelineClassificationId == GuidelineClassificationId
                    && ar.UseCaseId.ToString() == UseCaseId
                    && ar.UserGroupId.ToString() == this.GetUserGroupWithMostRights(GuidelineClassificationId, propertyId, UserGroups, accessRights, UseCaseId))
                .SingleOrDefault();
        }
    }
}