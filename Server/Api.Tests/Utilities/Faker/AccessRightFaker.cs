// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Bogus;
using InstanceService.Models;
using InstanceService.Models.Enum;

namespace InstanceService.Api.Tests.Utilities.Faker
{
    /// <summary>
    /// Faker for the <see cref="AccessRight"/> entity.
    /// </summary>
    public sealed class AccessRightFaker : Faker<AccessRight>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccessRightFaker"/> class.
        /// </summary>
        public AccessRightFaker()
        {
            RuleFor(ar => ar.Id, f => f.Random.Guid().ToString());
            RuleFor(ar => ar.Name, f => f.Lorem.Word());
            RuleFor(ar => ar.GuidelineClassificationId, f => f.Internet.Url());
            RuleFor(ar => ar.UserGroupId, f => Guid.NewGuid());
            RuleFor(ar => ar.UseCaseId, f => Guid.NewGuid());
            RuleFor(ar => ar.GuidlineClassificationPropertyId, f => f.Internet.Url());
            RuleFor(ar => ar.Right, f => f.PickRandom<PropertyRight>());
        }

        /// <summary>
        /// Sets the user group id of the <see cref="AccessRight"/>.
        /// </summary>
        /// <param name="userGroupId">The id of the user group for the entry to use.</param>
        /// <returns>The modified faker instance for chaining.</returns>
        public AccessRightFaker WithUserGroupId(string userGroupId)
        {
            RuleFor(x => x.UserGroupId, f => Guid.Parse(userGroupId));
            return this;
        }

        /// <summary>
        /// Sets the use case id of the <see cref="AccessRight"/>.
        /// </summary>
        /// <param name="useCaseId">The id of the use case for the entry to use.</param>
        /// <returns>The modified faker instance for chaining.</returns>
        public AccessRightFaker WithUseCaseId(string useCaseId)
        {
            RuleFor(x => x.UseCaseId, f => Guid.Parse(useCaseId));
            return this;
        }

        /// <summary>
        /// Sets the classification id of the <see cref="AccessRight"/>.
        /// </summary>
        /// <param name="classificationId">The id of the classification for the entry to use.</param>
        /// <returns>The modified faker instance for chaining.</returns>
        public AccessRightFaker WithClassificationId(string classificationId)
        {
            RuleFor(x => x.GuidelineClassificationId, f => classificationId);
            return this;
        }

        /// <summary>
        /// Sets the property id of the <see cref="AccessRight"/>.
        /// </summary>
        /// <param name="propertyId">The id of the property for the entry to use.</param>
        /// <returns>The modified faker instance for chaining.</returns>
        public AccessRightFaker WithPropertyId(string propertyId)
        {
            RuleFor(x => x.GuidlineClassificationPropertyId, f => propertyId);
            return this;
        }

        /// <summary>
        /// Sets the right of the <see cref="AccessRight"/>.
        /// </summary>
        /// <param name="right">The property right for the entry to use.</param>
        /// <returns>The modified faker instance for chaining.</returns>
        public AccessRightFaker WithRight(PropertyRight right)
        {
            RuleFor(x => x.Right, f => right);
            return this;
        }
    }
}