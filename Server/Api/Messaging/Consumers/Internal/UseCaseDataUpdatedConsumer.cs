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
using GuidelineModelIO;
using InstanceService.Api.Messaging.Consumers.Guidelines.Contracts;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;
using InstanceService.Models.Enum;
using Messaging.Core.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.RDF;

namespace InstanceService.Api.Messaging.Consumers.Internal;

/// <summary>
/// Represents a consumer for producing use case functionality with guideline data.
/// </summary>
public class UseCaseDataUpdatedConsumer : IInternalRequestConsumer<UseCaseGuidelineDataUpdated, UseCaseGuidelineDataUpdatedResponse>
{
    public ILogger<IInternalRequestConsumer<UseCaseGuidelineDataUpdated, UseCaseGuidelineDataUpdatedResponse>> Logger { get; }
    private readonly ILogger<IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>> _guidelineLogger;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IDynamicKafkaProducer _dynamicKafkaProducer;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IUseCaseFetcher _useCaseFetcher;
    private readonly IGuidelineProvider _guidelineProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UseCaseDataUpdatedConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="guidelineLogger">The guideline logger for GuidelineHelper.</param>
    /// <param name="accessRightsFetcher">The access rights fetcher.</param>
    /// <param name="dynamicKafkaProducer">The dynamic Kafka producer.</param>
    /// <param name="instanceRepository">The instance repository.</param>
    /// <param name="useCaseFetcher">The useCaseFetcher.</param>
    /// <param name="guidelineProvider">The guideline provider.</param>
    public UseCaseDataUpdatedConsumer(
        ILogger<IInternalRequestConsumer<UseCaseGuidelineDataUpdated, UseCaseGuidelineDataUpdatedResponse>> logger,
        ILogger<IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>> guidelineLogger,
        IAccessRightsFetcher accessRightsFetcher,
        IDynamicKafkaProducer dynamicKafkaProducer,
        IInstanceRepository instanceRepository,
        IUseCaseFetcher useCaseFetcher,
        IGuidelineProvider guidelineProvider)
    {
        Logger = logger;
        _guidelineLogger = guidelineLogger;
        _accessRightsFetcher = accessRightsFetcher;
        _dynamicKafkaProducer = dynamicKafkaProducer;
        _instanceRepository = instanceRepository;
        _useCaseFetcher = useCaseFetcher;
        _guidelineProvider = guidelineProvider;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve
        };
    }

    /// <summary>
    /// Consumes the produce use case with guidelines request.
    /// </summary>
    /// <param name="request">The produce use case with guidelines request.</param>
    public async Task<UseCaseGuidelineDataUpdatedResponse> ConsumeInternal(UseCaseGuidelineDataUpdated request)
    {
        // Fetch access rights and filter instances accessible by the current user for this use case
        var accessRights = await _accessRightsFetcher.GetAccessRightsAsync();
        var allInstances = await _instanceRepository.GetInstances(withMetadata: true);
        var relevantAccessRights = accessRights.ToList().FindAll(r => r.UseCaseId.ToString() == request.UseCaseId);

        // Only include instances the user can read/write based on their access rights
        var filteredInstances = allInstances.Where(instance =>
            relevantAccessRights.Any(accessRight =>
                accessRight.GuidelineClassificationId == instance.ClassificationId &&
                (accessRight.Right == PropertyRight.Write ||
                accessRight.Right == PropertyRight.Read))
        ).ToList();

        // Group instances into connected subgraphs - each component becomes one Kafka message
        var connectedComponents = GroupInstancesByConnectedComponents(filteredInstances);

        // Fetch full guideline and reduce it to only classifications/properties the user can access
        var guideline = await _guidelineProvider.GetGuideline(request.UseCaseId);
        var reducedGuideline = GuidelineHelper.GetReducedGuideline(_guidelineLogger, guideline, relevantAccessRights);

        var topicName = $"ekkodale.gaeco.instance.public.{request.UseCaseId}.gaecoExport";
        var headers = new Dictionary<string, object>
        {
            { "useCase", request.UseCaseId },
            { "usergroup", "gaeco" },
            { "version", "v1" },
            { "event", "dump" },
            { "entity", "GraphDataModel" }
        };

        List<GraphDataModel> graphDataModels = [];

         // For each connected component, build a complete graph model and send to Kafka
        foreach (var componentInstances in connectedComponents)
        {
            var graphDataModel = CreateGraphDataModelForComponent(componentInstances, relevantAccessRights);

            var externalUseCase = await _useCaseFetcher.GetUseCasesByIdAsync(request.UseCaseId);

            // Map external use case to local model
            var localUseCase = new UseCase
            {
                Id = externalUseCase.Id.ToString(),
                Name = externalUseCase.Name,
                Description = externalUseCase.Description
            };

            graphDataModel.AccessRights = relevantAccessRights;
            graphDataModel.UseCase = localUseCase;

            // Serialize guideline with to preserve $id/$ref/$type/$values metadata
            // This ensures the complex object graph survives the Kafka round trip with all relationships intact
            var serialized = JsonSerializer.Serialize(reducedGuideline, _jsonSerializerOptions);
            graphDataModel.Guidelines = JsonSerializer.Deserialize<object>(serialized, _jsonSerializerOptions);


            await _dynamicKafkaProducer.ProduceToDynamicTopicAsync(graphDataModel, topicName, headers);

            graphDataModels.Add(graphDataModel);

            Logger.LogInformation("Produced GraphDataModel to topic: {TopicName} with {InstanceCount} instances",
                topicName, componentInstances.Count);
        }

        UseCaseGuidelineDataUpdatedResponse response = new()
        {
            GraphDataModels = graphDataModels
        };

        Logger.LogInformation("Completed processing {TotalGraphs} connected graphs with guidelines for use case: {UseCaseId}",
            connectedComponents.Count, request.UseCaseId);

        return response;
    }

    /// <summary>
    /// Groups instances into connected components based on their relationships.
    /// </summary>
    /// <param name="instances">The instances to group.</param>
    /// <returns>A list of connected components, where each component is a list of connected instances.</returns>
    private static List<List<Instance>> GroupInstancesByConnectedComponents(List<Instance> instances)
    {
        var connectedComponents = new List<List<Instance>>();
        var visited = new HashSet<string>();
        var instanceLookup = instances.ToDictionary(i => i.Id, i => i);

        foreach (var instance in instances)
        {
            if (!visited.Contains(instance.Id))
            {
                var component = new List<Instance>();
                TraverseConnectedComponent(instance, instanceLookup, visited, component);
                connectedComponents.Add(component);
            }
        }

        return connectedComponents;
    }

    /// <summary>
    /// Recursively traverses and collects all instances in a connected component using depth-first search.
    /// </summary>
    /// <param name="currentInstance">The current instance being visited.</param>
    /// <param name="instanceLookup">Dictionary for quick instance lookup by ID.</param>
    /// <param name="visited">Set of already visited instance IDs.</param>
    /// <param name="component">The current component being built.</param>
    private static void TraverseConnectedComponent(Instance currentInstance, Dictionary<string, Instance> instanceLookup,
        HashSet<string> visited, List<Instance> component)
    {
        if (visited.Contains(currentInstance.Id))
            return;

        visited.Add(currentInstance.Id);
        component.Add(currentInstance);

        // Traverse all connected instances through relations
        foreach (var relation in currentInstance.Relations)
        {
            // Check subject connections (where current instance is the object)
            if (instanceLookup.TryGetValue(relation.SubjectId, out var subjectInstance) &&
                !visited.Contains(relation.SubjectId))
            {
                TraverseConnectedComponent(subjectInstance, instanceLookup, visited, component);
            }

            // Check object connections (where current instance is the subject)
            if (instanceLookup.TryGetValue(relation.ObjectId, out var objectInstance) &&
                !visited.Contains(relation.ObjectId))
            {
                TraverseConnectedComponent(objectInstance, instanceLookup, visited, component);
            }
        }
    }

    /// <summary>
    /// Creates a GraphDataModel for a specific connected component of instances.
    /// </summary>
    /// <param name="componentInstances">The instances in this connected component.</param>
    /// <param name="relevantAccessRights">Access rights used to filter instance properties (requires at least Read).</param>
    /// <returns>A GraphDataModel for the connected component.</returns>
    private GraphDataModel CreateGraphDataModelForComponent(List<Instance> componentInstances, List<AccessRight> relevantAccessRights)
    {
        var graphDataModel = new GraphDataModel
        {
            GraphTemplate = "@prefix ex: <http://example.org/> .\nNode1 hasRelation Node2 .",
        };

        var metaDataNodes = componentInstances
            .Where(instance => !string.IsNullOrWhiteSpace(instance.Id) && !string.IsNullOrWhiteSpace(instance.ClassificationId))
            .Select(instance => new MetaDataNode
            {
                Id = instance.Id,
                ClassType = instance.ClassificationId,
                PropertiesValues = FilterPropertiesByAccessRights(instance.Properties, instance.ClassificationId, relevantAccessRights)
            })
            .ToList();

        if (metaDataNodes.Count > 0)
        {
            graphDataModel.GraphMetadata = metaDataNodes;
        }

        var relations = new List<Triple>();
        var graph = new Graph();
        var componentInstanceIds = new HashSet<string>(componentInstances.Select(i => i.Id));

        foreach (var instance in componentInstances)
        {
            foreach (var relation in instance.Relations)
            {
                if (componentInstanceIds.Contains(relation.SubjectId) && componentInstanceIds.Contains(relation.ObjectId))
                {
                    var subjectNode = graph.CreateUriNode(new Uri($"http://example.org/instances/{relation.SubjectId}"));

                    // The relation is identified by its canonical ontology property URI, which is used
                    // directly as the RDF predicate. Fall back to a synthetic URI only for legacy/empty values.
                    IUriNode predicateNode;
                    if (!string.IsNullOrEmpty(relation.PredicateUri) && Uri.TryCreate(relation.PredicateUri, UriKind.Absolute, out Uri? predicateUri) && predicateUri != null)
                    {
                        predicateNode = graph.CreateUriNode(predicateUri);
                    }
                    else
                    {
                        predicateNode = graph.CreateUriNode(new Uri($"http://example.org/relations/{relation.PredicateUri}"));
                    }

                    var objectNode = graph.CreateUriNode(new Uri($"http://example.org/instances/{relation.ObjectId}"));

                    relations.Add(new Triple(subjectNode, predicateNode, objectNode));
                }
            }
        }

        graphDataModel.GraphData = GraphProcessingService.ConvertRelationsToTurtle(relations, graph);

        return graphDataModel;
    }

    /// <summary>
    /// Filters instance properties by access rights so that only properties with at least Read permission remain.
    /// </summary>
    /// <param name="properties">The original instance properties.</param>
    /// <param name="classificationId">The instance classification id.</param>
    /// <param name="relevantAccessRights">Access rights relevant to the current use case.</param>
    /// <returns>A filtered property dictionary.</returns>
    private static Dictionary<string, string> FilterPropertiesByAccessRights(
        Dictionary<string, string>? properties,
        string classificationId,
        List<AccessRight> relevantAccessRights)
    {
        if (string.IsNullOrWhiteSpace(classificationId) || properties == null || properties.Count == 0)
            return new Dictionary<string, string>();

        var allowedPropertyIds = relevantAccessRights
            .Where(ar => ar.GuidelineClassificationId == classificationId &&
                (ar.Right == PropertyRight.Read || ar.Right == PropertyRight.Write))
            .Select(ar => ar.Name)
            .ToHashSet();

        return properties
            .Where(kvp => allowedPropertyIds.Contains(kvp.Key) &&
                !string.IsNullOrWhiteSpace(kvp.Key) &&
                !string.IsNullOrWhiteSpace(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
