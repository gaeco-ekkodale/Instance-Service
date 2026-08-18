// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using InstanceService.Models.Enum;
using Messaging.Core.Abstractions;
using InstanceService.Models;


namespace InstanceService.Api.Messaging.Consumers.Internal;

/// <summary>
/// Represents a consumer for handling GetGraphRequest messages internally.
/// </summary>
public class GetGraphRequestConsumer : IInternalRequestConsumer<GetGraphRequest, GetGraphResponse>
{
    public ILogger<IInternalRequestConsumer<GetGraphRequest, GetGraphResponse>> Logger { get; }

    private readonly IInstanceRepository _repository;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IOntologyProvider _ontologyProvider;

    public GetGraphRequestConsumer(
        ILogger<IInternalRequestConsumer<GetGraphRequest, GetGraphResponse>> logger,
        IInstanceRepository repository,
        IAccessRightsFetcher accessRightsFetcher,
        IOntologyProvider ontologyProvider)
    {
        Logger = logger;
        _repository = repository;
        _accessRightsFetcher = accessRightsFetcher;
        _ontologyProvider = ontologyProvider;
    }

    /// <summary>
    /// Handles the internal consumption of GetGraphRequest messages.
    /// </summary>
    /// <param name="request">The GetGraphRequest message.</param>
    /// <returns>The GetGraphResponse message.</returns>
    public async Task<GetGraphResponse> ConsumeInternal(GetGraphRequest request)
    {
        // Get instances and access rights
        IEnumerable<Models.Instance> instances = await _repository.GetInstances();
        IEnumerable<AccessRight> accessRights = await _accessRightsFetcher.GetAccessRightsAsync();

        // Filter access rights by UseCaseId and get relevant instance IDs
        var relevantInstanceIds = accessRights
            .Where(accessRight => accessRight.UseCaseId.ToString() == request.UseCaseId)
            .Select(accessRight => accessRight.GuidelineClassificationId)
            .Distinct();

        // Filter instances to include only those with relevant classification IDs
        var filteredInstances = instances
            .Where(instance => relevantInstanceIds.Contains(instance.ClassificationId))
            .ToList();

        // Determine accessibility for each filtered instance
        var instanceTuples = new List<(Models.Instance, Accessibility)>();

        foreach (var instance in filteredInstances)
        {
            var propertyRights = accessRights
                .Where(accessRight => accessRight.GuidelineClassificationId == instance.ClassificationId)
                .Where(accessRight => accessRight.UseCaseId.ToString() == request.UseCaseId)
                .Select(accessRight => accessRight.Right)
                .ToList();

            Accessibility accessibility;

            if (propertyRights.Any() && propertyRights.All(right => right == PropertyRight.Write))
            {
                accessibility = Accessibility.FullControl;
            }
            else if (propertyRights.Any(right => right == PropertyRight.Read) && propertyRights.Any(right => right == PropertyRight.Write))
            {
                accessibility = Accessibility.ReadWrite;
            }
            else if (propertyRights.Any(right => right == PropertyRight.Read) && propertyRights.All(right => right != PropertyRight.Write))
            {
                accessibility = Accessibility.ReadOnly;
            }
            else
            {
                accessibility = Accessibility.None;
            }

            instanceTuples.Add((instance, accessibility));
        }

        // Get all relations and filter them based on instance IDs
        IEnumerable<Models.InstanceRelation> allRelations = instances.SelectMany(i => i.Relations).Distinct();

        var instanceIds = filteredInstances.Select(instance => instance.Id).ToHashSet();

        var filteredRelations = allRelations
            .Where(relation => instanceIds.Contains(relation.SubjectId) || instanceIds.Contains(relation.ObjectId))
            .ToList();

        await EnrichRelationLabelsAsync(filteredRelations);

        // Create the response
        GetGraphResponse response = new()
        {
            Instances = instanceTuples,
            Relations = filteredRelations
        };

        return response;
    }

    /// <summary>
    /// Sets the optional display <see cref="Models.InstanceRelation.Label"/> on each relation from the
    /// ontology, keyed by its predicate URI. Relations stay identified by URI; the label is display-only.
    /// </summary>
    private async Task EnrichRelationLabelsAsync(IEnumerable<Models.InstanceRelation> relations)
    {
        var labelByUri = await _ontologyProvider.GetRelationLabelsAsync();

        foreach (var relation in relations)
        {
            if (!string.IsNullOrEmpty(relation.PredicateUri) && labelByUri.TryGetValue(relation.PredicateUri, out var label))
                relation.Label = label;
        }
    }
}
