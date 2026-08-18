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
using InstanceService.Api.Messaging.Consumers.Classifications.Contracts;
using InstanceService.Api.Services;
using GuidelineModel = Guideline.Model.Model;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using Messaging.Core.Abstractions;
using InstanceService.Models;
using InstanceService.Models.Enum;

namespace InstanceService.Api.Messaging.Consumers.Classifications
{
    public class GetClassificationConsumer(ILogger<IInternalRequestConsumer<GetClassification,
        InstanceService.Models.Classification>> logger,
        IGuidelineReconstructionService reconstruction,
        IAccessRightsFetcher accessRightsFetcher,
        IUserGroupProvider userGroupProvider,
        IAccessRightHelper accessRightHelper) : IInternalRequestConsumer<GetClassification, InstanceService.Models.Classification>
    {
        public ILogger<IInternalRequestConsumer<GetClassification, InstanceService.Models.Classification>> Logger { get; } = logger;

        private readonly IGuidelineReconstructionService _reconstruction = reconstruction;
        private readonly IAccessRightsFetcher _accessRightsFetcher = accessRightsFetcher;
        private readonly IUserGroupProvider _userGroupProvider = userGroupProvider;
        private readonly IAccessRightHelper _accessRightHelper = accessRightHelper;

        /// <summary>
        /// Consumes the GetClassification request and returns the corresponding classification.
        /// </summary>
        /// <param name="request">The GetClassification request message containing the classification ID.</param>
        /// <returns>A Task representing the asynchronous operation, containing the mapped Classification.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the classification file is not found.</exception>
        /// <exception cref="Exception">Thrown for any unexpected errors during processing.</exception>
        public async Task<InstanceService.Models.Classification> ConsumeInternal(GetClassification request)
        {
            try
            {
                // Fetch required data
                var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
                var userGroups = await _userGroupProvider.GetUserGroupIdsAsync(request.Token);
                var useCaseId = request.UseCaseId;

                // Reconstruct only the requested classification (with its properties) — not the whole guideline.
                var decodedId = Uri.UnescapeDataString(request.ClassificationId);
                var matchingClassification = await _reconstruction.GetClassificationAsync(decodedId);

                if (matchingClassification == null)
                {
                    return null;
                }

                // Check if the user has access to this classification
                if (!_accessRightHelper.HasAccessibility(matchingClassification.Identifier.ToString(), userGroups, accessRights, useCaseId))
                {
                    return null;
                }

                // Map all property sets from the classification
                var propertySets = matchingClassification.ClassificationProperties
                    .GroupBy(cp => cp.PropertySet)
                    .Select(g => MapPropertySet(g.Key, g, Enumerable.Empty<IProperty>(), accessRights, decodedId, useCaseId, userGroups))
                    .ToList();

                var classification = new InstanceService.Models.Classification
                {
                    Id = matchingClassification.Identifier.ToString(),
                    Name = matchingClassification.Name,
                    Right = GetClassificationRight(propertySets.Select(ps => ps.Right)),
                    PropertySets = propertySets
                };

                return classification;
            }
            catch (FileNotFoundException ex)
            {
                Logger.LogError(ex, "The classification file was not found.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred while fetching classifications.");
                throw;
            }
        }

        /// <summary>
        /// Maps a property set from the classification to a PropertySet.
        /// </summary>
        /// <param name="propertySet">The property set to be mapped.</param>
        /// <param name="classificationProperties">The classification properties associated with the property set.</param>
        /// <param name="allProperties">All properties in the domain for reference.</param>
        /// <param name="accessRights">The access rights applicable to the properties.</param>
        /// <param name="GuidelineClassificationId">The ID of the guideline classification for filtering access rights.</param>
        /// <param name="UseCaseId">The ID of the use case for filtering access rights.</param>
        /// <param name="UserGroups">The list of user groups for determining access rights.</param>
        /// <returns>The mapped PropertySet.</returns>
        private InstanceService.Models.PropertySet MapPropertySet(IPropertySet propertySet, IEnumerable<IClassificationProperty> classificationProperties, IEnumerable<IProperty> allProperties, IEnumerable<AccessRight> accessRights, string GuidelineClassificationId, string UseCaseId, List<string> UserGroups)
        {
            // TODO: Improve workaround for properties withoutPropertySet
            if (propertySet == null)
            {
                var propertiesWithoutPropertySet = classificationProperties
                .Where(cp => cp.PropertySet == null)
                .Select(cp => MapProperty(cp, accessRights, GuidelineClassificationId, UseCaseId, UserGroups))
                .ToList();


                return new InstanceService.Models.PropertySet
                {
                    Id = "NoPropertySet",
                    Name = "NoPropertySet",
                    Properties = propertiesWithoutPropertySet,
                    Right = GetSetRight(propertiesWithoutPropertySet.Select(p => p.Right))
                };
            }

            // Map classificationProperties to properties
            var properties = classificationProperties
                .Where(cp => cp.PropertySet.Identifier == propertySet.Identifier)
                .Select(cp => MapProperty(cp, accessRights, GuidelineClassificationId, UseCaseId, UserGroups))
                .ToList();

            // Create a PropertySet
            return new InstanceService.Models.PropertySet
            {
                Id = propertySet.Identifier.ToString(),
                Name = propertySet.Name,
                Properties = properties,
                Right = GetSetRight(properties.Select(p => p.Right))
            };
        }

        /// <summary>
        /// Maps a property to a Property.
        /// </summary>
        /// <param name="property">The property to be mapped.</param>
        /// <param name="accessRights">The access rights applicable to the property.</param>
        /// <param name="GuidelineClassificationId">The ID of the guideline classification for filtering access rights.</param>
        /// <param name="UseCaseId">The ID of the use case for filtering access rights.</param>
        /// <param name="UserGroups">The list of user groups for determining the user group with most rights.</param>
        /// <returns>The mapped Property.</returns>
        private InstanceService.Models.Property MapProperty(IClassificationProperty classificationProperty, IEnumerable<AccessRight> accessRights, string GuidelineClassificationId, string UseCaseId, List<string> UserGroups)
        {
            var property = classificationProperty.PropertyAssignment?.Property;

            var propertyType = property?.GetType().Name ?? nameof(GuidelineModel.PropertySimple);

            IEnumerable<InstanceService.Models.PropertyEnumValue> enumValues = [];
            if (property is GuidelineModel.PropertyEnum pe && pe.Enums != null)
            {
                if (classificationProperty.PropertyAssignment is GuidelineModel.PropertyEnumAssignment pea && pea.SelectedEnum != null)
                {
                    var fullItem = pe.Enums.FirstOrDefault(e => e.ID == pea.SelectedEnum.ID);
                    enumValues = fullItem != null
                        ? fullItem.Values.Select(v => new InstanceService.Models.PropertyEnumValue { Id = v.Key, Name = v.Value })
                        : pe.Enums.SelectMany(e => e.Values.Select(v => new InstanceService.Models.PropertyEnumValue { Id = v.Key, Name = v.Value }));
                }
                else
                {
                    enumValues = pe.Enums.SelectMany(e => e.Values.Select(v => new InstanceService.Models.PropertyEnumValue { Id = v.Key, Name = v.Value }));
                }
            }

            // Map accessRights to property access rights
            var propertyAccessRight = accessRights
                .Where(ar => ar.GuidlineClassificationPropertyId == property?.Identifier
                    && ar.GuidelineClassificationId == GuidelineClassificationId
                    && ar.UseCaseId.ToString() == UseCaseId
                    && ar.UserGroupId.ToString() == _accessRightHelper.GetUserGroupWithMostRights(GuidelineClassificationId, property?.Identifier, UserGroups, accessRights, UseCaseId))
                .SingleOrDefault();

            return new InstanceService.Models.Property
            {
                Id = property?.Identifier?.ToString() ?? string.Empty,
                Name = property?.Name ?? string.Empty,
                Value = "",
                StorageType = property?.StorageType ?? Guideline.Model.Enums.StorageType.String,
                Right = propertyAccessRight == null ? PropertyRight.None : propertyAccessRight.Right,
                PropertyType = propertyType,
                EnumValues = enumValues,
            };
        }

        /// <summary>
        /// Determines the access right for a property set based on the rights of its properties.
        /// </summary>
        /// <param name="rights">The rights of the properties within the property set.</param>
        /// <returns>The determined PropertySetRight.</returns>
        private PropertySetRight GetSetRight(IEnumerable<PropertyRight> rights)
        {
            bool hasWrite = rights.Any(r => r == PropertyRight.Write);
            bool hasRead = rights.Any(r => r == PropertyRight.Read);

            if (hasWrite && hasRead)
            {
                return PropertySetRight.Mixed;
            }

            if (hasWrite)
            {
                return PropertySetRight.Write;
            }

            if (hasRead && rights.All(r => r == PropertyRight.Read))
            {
                return PropertySetRight.Read;
            }

            return PropertySetRight.None;
        }

        /// <summary>
        /// Determines the access right for a classification based on the rights of its property sets.
        /// </summary>
        /// <param name="propertySetRights">The rights of the property sets within the classification.</param>
        /// <returns>The determined ClassificationRight.</returns>
        private ClassificationRight GetClassificationRight(IEnumerable<PropertySetRight> propertySetRights)
        {
            if (propertySetRights.Any(psr => psr == PropertySetRight.Mixed))
            {
                return ClassificationRight.Mixed;
            }

            if (propertySetRights.All(psr => psr == PropertySetRight.Write))
            {
                return ClassificationRight.Write;
            }

            if (propertySetRights.Any(psr => psr == PropertySetRight.Read))
            {
                return ClassificationRight.Read;
            }

            return ClassificationRight.None;
        }
    }
}