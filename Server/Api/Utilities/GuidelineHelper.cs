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

namespace InstanceService.Api.Utilities;

/// <summary>
/// Provides helper methods for operations related to guidelines.
/// </summary>
public static class GuidelineHelper
{
	/// <summary>
	/// Creates a new, reduced guideline based on a given set of access rights.
	/// </summary>
	/// <remarks>
	/// This method filters the classifications, properties, and property sets of the original guideline
	/// to include only those elements for which access is granted according to the <paramref name="accessRights"/>.
	/// </remarks>
	/// <param name="logger">The logger to use for logging any errors that occur during the process.</param>
	/// <param name="guideLine">The original, complete guideline object to be filtered.</param>
	/// <param name="accessRights">An enumeration of access rights that define which parts of the guideline are accessible.</param>
	/// <returns>A new <see cref="IGuideline"/> instance containing only the permitted elements.</returns>
	/// <exception cref="InvalidOperationException">Thrown when an error occurs during the guideline reduction process.</exception>
	public static IGuideline GetReducedGuideline(ILogger logger, IGuideline guideLine, IEnumerable<AccessRight> accessRights)
	{
		try
		{
			Guideline.Model.Model.Guideline reducedGuideline = new()
			{
				ID = guideLine.ID,
				Name = guideLine.Name,
				Identifier = guideLine.Identifier,
				Description = guideLine.Description,
				ComplexData = guideLine.ComplexData,
				Definition = guideLine.Definition,
				Status = guideLine.Status,
				Version = guideLine.Version,
			};

			Guideline.Model.Model.Domain reducedGuidelineDomain = new()
			{
				ID = guideLine.Domain.ID,
				Name = guideLine.Domain.Name,
				Identifier = guideLine.Domain.Identifier,
				Description = guideLine.Domain.Description,
				Definition = guideLine.Domain.Definition,
				Status = guideLine.Domain.Status,
				Version = guideLine.Domain.Version,
			};

			// Pre-compute allowed classification IDs and property IDs for efficient lookup
			var allowedClassificationIds = accessRights
				.Select(ar => ar.GuidelineClassificationId)
				.ToHashSet();

			// Filter classifications and create defensive copies to avoid mutating the cached guideline.
			// The guideline is cached as a shared in-memory object (CacheService/IMemoryCache).
			// Previously, ClassificationProperties were set directly on the original objects,
			// which permanently corrupted the cache after the first call.
			reducedGuidelineDomain.Classifications = guideLine.Domain.Classifications
				.Where(cls => allowedClassificationIds.Contains(cls.Identifier))
				.Select(cls =>
				{
					var allowedPropertyIds = accessRights
						.Where(ar => ar.GuidelineClassificationId == cls.Identifier)
						.Select(ar => ar.GuidlineClassificationPropertyId)
						.ToHashSet();

					return new Guideline.Model.Model.Classification
					{
						ID = cls.ID,
						Name = cls.Name,
						Code = cls.Code,
						Identifier = cls.Identifier,
						Description = cls.Description,
						Definition = cls.Definition,
						Status = cls.Status,
						Version = cls.Version,
						Parent = cls.Parent,
						Children = cls.Children,
						ClassificationProperties = cls.ClassificationProperties
							.Where(p => allowedPropertyIds.Contains(p.Identifier))
							.ToList(),
					};
				})
				.ToList<IClassification>();

			// Filter properties
			var allowedDomainPropertyIds = accessRights
				.Select(ar => ar.GuidlineClassificationPropertyId)
				.ToHashSet();
			reducedGuidelineDomain.Properties = (ICollection<IProperty>)guideLine.Domain.Properties
				.Where(prop => allowedDomainPropertyIds.Contains(prop.Identifier))
				.ToList();

			// Find all property set identifiers that are used in reduced guideline's classification properties.
			var propertySetIdentifiers = reducedGuidelineDomain.Classifications
				.SelectMany(cls => cls.ClassificationProperties)
				.Where(prop => prop.PropertySet != null) // Ensure PropertySet is not null to avoid NullReferenceException
				.Select(clsProp => clsProp.PropertySet.Identifier)
				.Distinct();

			// Filter property sets from the original guideline (not reducedGuidelineDomain which has no PropertySets yet)
			var propertySetIdentifierSet = new HashSet<string>(propertySetIdentifiers.Select(id => id.ToString()));
			reducedGuidelineDomain.PropertySets = guideLine.Domain.PropertySets
				.Where(pset => propertySetIdentifierSet.Contains(pset.Identifier.ToString()))
				.ToList();

			// Assign reduced domain to reduced guideline's domain
			reducedGuideline.Domain = reducedGuidelineDomain;

			return reducedGuideline;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred while reducing guideline. Guideline ID: {GuidelineID}, Name: {GuidelineName}", guideLine.ID, guideLine.Name);
			throw new InvalidOperationException($"Error occurred while reducing guideline. Guideline ID: {guideLine.ID}, Name: {guideLine.Name}", ex);
		}
	}
}
