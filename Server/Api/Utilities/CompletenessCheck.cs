// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using GuidelineModelIO;
using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;
using InstanceService.Models.Enum;
using Messaging.Core.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using VDS.RDF;

namespace InstanceService.Api.Utilities;

/// <summary>
/// Validates the completeness of graph data based on use case requirements
/// </summary>
public class CompletenessCheck : ICompletenessCheck
{
    private readonly IGraphQueryExecutor _graphQueryExecutor;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IInstanceRepository _instanceRepository;
    private readonly ILogger<CompletenessCheck> _logger;
    private readonly IDynamicKafkaProducer _dynamicKafkaProducer;
    private readonly ConcurrentDictionary<string, List<string>> _useCaseClassificationMap = new();
    private IEnumerable<AccessRight>? _accessRightsCache;
    private readonly IGuidelineProvider _guidelineProvider;

    /// <summary>
    /// Initializes a new instance of the CompletenessCheck class
    /// </summary>
    public CompletenessCheck(
        IGraphQueryExecutor graphQueryExecutor,
        IAccessRightsFetcher accessRightsFetcher,
        IInstanceRepository instanceRepository,
        ILogger<CompletenessCheck> logger,
        IDynamicKafkaProducer dynamicKafkaProducer,
        IGuidelineProvider guidelineProvider)
    {
        _graphQueryExecutor = graphQueryExecutor;
        _accessRightsFetcher = accessRightsFetcher;
        _instanceRepository = instanceRepository;
        _logger = logger;
        _dynamicKafkaProducer = dynamicKafkaProducer;
        _guidelineProvider = guidelineProvider;
    }

    /// <summary>
    /// Checks all use cases for completeness for a given instance
    /// </summary>
    /// <param name="instanceId">The ID of the instance to check</param>
    public async Task CheckAndSendAsync(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return;

        await InitializeUseCaseClassificationMapAsync();

        // Check all use cases for completeness
        foreach (var (useCaseId, relevantClasses) in _useCaseClassificationMap)
        {
            if (await IsUseCaseCompleteAsync(instanceId, useCaseId))
            {
                var subgraphInstances = await _graphQueryExecutor.ExecuteCompletenessQueryAsync(instanceId, relevantClasses);

                _logger.LogInformation("Use case {UseCaseId} for instance {InstanceId} is complete", useCaseId, instanceId);
                await SendGraphDataAsync(instanceId, useCaseId, subgraphInstances);
            }
        }
    }

    /// <summary>
    /// Checks all use cases for completeness for multiple instances
    /// </summary>
    /// <param name="instanceIds">Array of instance IDs to check</param>
    public async Task CheckAndSendAsync(string[] instanceIds)
    {
        if (instanceIds == null || instanceIds.Length == 0)
        {
            _logger.LogWarning("No instance IDs provided for completeness check");
            return;
        }

        _logger.LogInformation("Starting completeness check for {Count} instances", instanceIds.Length);
        await InitializeUseCaseClassificationMapAsync();

        var sentSubgraphsByUseCase = _useCaseClassificationMap.Keys
            .ToDictionary(useCaseId => useCaseId, _ => new HashSet<string>());
        var totalMessagesCount = 0;

        foreach (var instanceId in instanceIds.Where(id => !string.IsNullOrEmpty(id)))
        {
            foreach (var (useCaseId, relevantClasses) in _useCaseClassificationMap)
            {
                if (sentSubgraphsByUseCase[useCaseId].Contains(instanceId))
                    continue;

                try
                {
                    if (await ProcessInstanceForCompleteness(instanceId, useCaseId, relevantClasses, sentSubgraphsByUseCase[useCaseId]))
                    {
                        totalMessagesCount++;
                        _logger.LogInformation("Use case {UseCaseId} complete for instance {InstanceId}", useCaseId, instanceId);
                    }
                }
                catch (Exception ex)
                {
                    // Logged per instance so the rest of the batch still gets checked.
                    _logger.LogError(ex, "Completeness check failed for use case {UseCaseId} and instance {InstanceId}",
                        useCaseId, instanceId);
                }
            }
        }

        _logger.LogInformation("Completed check for {Count} instances. Sent {MessageCount} messages",
            instanceIds.Length, totalMessagesCount);
    }

    /// <summary>
    /// Checks if a graph is complete for a specific use case based on an instance ID
    /// </summary>
    /// <param name="instanceId">The ID of the instance to check</param>
    /// <param name="useCaseId">The ID of the use case</param>
    /// <returns>True if the graph is complete for the use case, otherwise false</returns>
    public async Task<bool> IsUseCaseCompleteAsync(string instanceId, string useCaseId)
    {
        if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(useCaseId))
            return false;

        await InitializeUseCaseClassificationMapAsync();

        if (!_useCaseClassificationMap.TryGetValue(useCaseId, out var relevantClasses) || !relevantClasses.Any())
            return false;

        var instance = await _instanceRepository.GetInstance(instanceId);
        if (instance == null || !relevantClasses.Contains(instance.ClassificationId))
            return false;

        var queryResult = await _graphQueryExecutor.ExecuteCompletenessQueryAsync(instanceId, relevantClasses);
        if (!queryResult.Any())
            throw new InvalidOperationException($"No related instances found for instance {instanceId} and use case {useCaseId}");

        var presentClassifications = queryResult.Select(node => node.ClassificationId).ToHashSet();
        if (!relevantClasses.All(presentClassifications.Contains))
            return false;

        return await AreAllPropertiesCompleteAsync(queryResult, useCaseId);
    }

    /// <summary>
    /// Finds all complete subgraphs for a specific use case without requiring a start instance.
    /// Each complete subgraph will be sent as a separate Kafka message.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case to check</param>
    /// <returns>List of root instance IDs that form complete subgraphs</returns>
    public async Task<List<string>> FindAndSendCompleteSubgraphsAsync(string useCaseId)
    {
        if (string.IsNullOrEmpty(useCaseId))
        {
            _logger.LogWarning("Cannot find complete subgraphs: Use case ID is empty");
            return new List<string>();
        }

        try
        {
            _logger.LogInformation("Searching for complete subgraphs for use case {UseCaseId}", useCaseId);
            await InitializeUseCaseClassificationMapAsync();

            if (!_useCaseClassificationMap.TryGetValue(useCaseId, out var relevantClasses) || !relevantClasses.Any())
            {
                _logger.LogWarning("No relevant classes found for use case {UseCaseId}", useCaseId);
                return new List<string>();
            }

            var candidateInstances = await _graphQueryExecutor.FindCandidateInstancesAsync(relevantClasses);
            if (candidateInstances == null || !candidateInstances.Any())
            {
                _logger.LogInformation("No candidate instances found for use case {UseCaseId}", useCaseId);
                return new List<string>();
            }

            var processedInstances = new HashSet<string>();
            var completeSubgraphRoots = new List<string>();

            foreach (var instanceId in candidateInstances.Select(i => i.Id))
            {
                if (processedInstances.Contains(instanceId))
                    continue;

                if (await ProcessInstanceForCompleteness(instanceId, useCaseId, relevantClasses, processedInstances))
                {
                    completeSubgraphRoots.Add(instanceId);
                    _logger.LogInformation("Complete subgraph for {UseCaseId} from {InstanceId}", useCaseId, instanceId);
                }
                else
                {
                    processedInstances.Add(instanceId);
                }
            }

            _logger.LogInformation("Found and sent {Count} complete subgraphs for use case {UseCaseId}",
        completeSubgraphRoots.Count, useCaseId);

            return completeSubgraphRoots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding complete subgraphs for use case {UseCaseId}", useCaseId);
            return new List<string>();
        }
    }

    public async Task<IEnumerable<string>> GetAllUseCaseIdsAsync()
    {
        await InitializeUseCaseClassificationMapAsync();
        return _useCaseClassificationMap.Keys;
    }

    /// <summary>
    /// Retrieves access rights or uses the cache
    /// </summary>
    private async Task<IEnumerable<AccessRight>> GetAccessRightsAsync()
    {
        return _accessRightsCache ??= await _accessRightsFetcher.GetAccessRightsAsync();
    }

    /// <summary>
    /// Initializes the mapping between use cases and their relevant classifications
    /// </summary>
    private async Task InitializeUseCaseClassificationMapAsync()
    {
        if (_useCaseClassificationMap.Any())
            return;

        try
        {
            var allAccessRights = await GetAccessRightsAsync();
            var useCaseGroups = allAccessRights.GroupBy(ar => ar.UseCaseId.ToString());

            foreach (var group in useCaseGroups)
            {
                _useCaseClassificationMap[group.Key] = group
                  .Select(ar => (string)ar.GuidelineClassificationId)
            .Distinct()
                            .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing use case mappings");
        }
    }

    private async Task<bool> ProcessInstanceForCompleteness(
        string instanceId,
        string useCaseId,
        List<string> relevantClasses,
        HashSet<string> processedInstances)
    {
        if (!await IsUseCaseCompleteAsync(instanceId, useCaseId))
            return false;

        var subgraphInstances = await _graphQueryExecutor.ExecuteCompletenessQueryAsync(instanceId, relevantClasses);

        foreach (var instance in subgraphInstances)
            processedInstances.Add(instance.Id);

        await SendGraphDataAsync(instanceId, useCaseId, subgraphInstances);
        return true;
    }

    /// <summary>
    /// Checks all use cases for completeness for a given instance
    /// </summary>
    /// <param name="instanceId">The ID of the instance to check</param>
    /// <param name="useCaseId">The ID of the use case</param>
    /// <param name="subgraphInstances">Optional pre-filtered subgraph instances. If null, instances will be queried.</param>
    private async Task SendGraphDataAsync(string instanceId, string useCaseId, IEnumerable<Models.Instance> subgraphInstances)
    {
        if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(useCaseId))
            throw new ArgumentException("Instance ID and Use Case ID must be provided");

        ArgumentNullException.ThrowIfNull(subgraphInstances);

        try
        {
            var topicName = $"ekkodale.gaeco.instance.public.{useCaseId}.gaecoExport";
            var headers = new Dictionary<string, object>
            {
                { "useCase", useCaseId },
                { "usergroup", "gaeco" },
                { "version", "v1" },
                { "event", "dump" },
                { "entity", "GraphDataModel" }
            };

            var accessRights = await GetAccessRightsAsync();
            var relevantAccessRights = accessRights.Where(ar => ar.UseCaseId.ToString() == useCaseId).ToList();

            var metaDataNodes = subgraphInstances.Select(node => new MetaDataNode
            {
                Id = node.Id,
                ClassType = node.ClassificationId,
                PropertiesValues = node.Properties,
            }).ToList();

            // Build graph data inline
            var relations = new List<Triple>();
            var graph = new Graph();
            var instanceIds = subgraphInstances.Select(i => i.Id).ToHashSet();
            graph.NamespaceMap.AddNamespace("inst", new Uri("https://example.com/"));

            foreach (var instance in subgraphInstances)
            {
                foreach (var relation in instance.Relations.Where(r =>
                instanceIds.Contains(r.SubjectId) && instanceIds.Contains(r.ObjectId)))
                {
                    var subjectNode = graph.CreateUriNode(new Uri($"http://example.org/instances/{relation.SubjectId}"));
                    var objectNode = graph.CreateUriNode(new Uri($"http://example.org/instances/{relation.ObjectId}"));

                    var predicateNode = !string.IsNullOrEmpty(relation.PredicateUri) &&
                     Uri.TryCreate(relation.PredicateUri, UriKind.Absolute, out Uri? predicateUri) &&
                      predicateUri != null
                        ? graph.CreateUriNode(predicateUri)
                            : graph.CreateUriNode(new Uri($"http://example.org/relations/{relation.PredicateUri}"));

                    relations.Add(new Triple(subjectNode, predicateNode, objectNode));
                }
            }

            var guideline = await _guidelineProvider.GetGuideline(useCaseId);
            var reducedGuideline = GuidelineHelper.GetReducedGuideline(
                _logger, 
                guideline, 
                relevantAccessRights);

            var graphDataModel = new GraphDataModel
            {
                GraphTemplate = "@prefix ex: <http://example.org/> .\nNode1 hasRelation Node2 .",
                GraphMetadata = metaDataNodes,
                GraphData = GraphProcessingService.ConvertRelationsToTurtle(relations, graph),
                UseCase = new UseCase { Id = useCaseId },
                AccessRights = relevantAccessRights,
                Guidelines = new List<object> { reducedGuideline }
            };

            await _dynamicKafkaProducer.ProduceToDynamicTopicAsync(
                new List<GraphDataModel> { graphDataModel },
                topicName,
                headers);

            _logger.LogInformation("Sent message for use case {UseCaseId} with {InstanceCount} instances",
                      useCaseId, metaDataNodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message for use case {UseCaseId}", useCaseId);
        }
    }

    /// <summary>
    /// Checks if all required properties for each instance are filled
    /// </summary>
    /// <param name="instances">Collection of instances to check</param>
    /// <param name="useCaseId">The ID of the use case</param>
    /// <returns>True if all properties are complete, otherwise false</returns>
    private async Task<bool> AreAllPropertiesCompleteAsync(IEnumerable<Models.Instance> instances, string useCaseId)
    {
        var allAccessRights = await GetAccessRightsAsync();
        var accessRightsList = allAccessRights.Where(ar => ar.UseCaseId.ToString() == useCaseId).ToList();

        if (!accessRightsList.Any())
            return false;

        var requiredPropertiesByClassification = accessRightsList
            .Where(ar => ar.Right == PropertyRight.Read)
            .GroupBy(ar => ar.GuidelineClassificationId)
            .ToDictionary(
            g => g.Key,
            g => g.Select(ar => ar.Name).Distinct().ToList()
);

        foreach (var instance in instances)
        {
            if (!requiredPropertiesByClassification.TryGetValue(instance.ClassificationId, out var requiredProperties))
                continue;

            if (instance.Properties == null ||
                      requiredProperties.Any(propertyName =>
                     !instance.Properties.TryGetValue(propertyName, out string propertyValue) ||
                   string.IsNullOrEmpty(propertyValue)))
            {
                return false;
            }
        }

        return true;
    }
}