// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Options;
using InstanceService.Api.Services;
using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace InstanceService.Api.Messaging.Consumers;

public class GraphDataModelConsumer(ILogger<GraphDataModelConsumer> logger,
    IServiceProvider serviceProvider,
    IOptions<KafkaOptions> kafkaOptions)
    : KafkaConsumerBase(logger, serviceProvider, kafkaOptions, "GraphDataModel")
{
    /// <summary>
    /// Processes a Kafka message by deserializing it into a <see cref="GraphDataModel"/> and processing it.
    /// </summary>
    /// <param name="messageValue">The string value of the Kafka message.</param>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected override async Task ProcessMessage(string messageValue, CancellationToken stoppingToken)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var graphDataModel = JsonSerializer.Deserialize<GraphDataModel>(messageValue, options);
            if (graphDataModel == null)
            {
                _logger.LogWarning("Failed to deserialize message to GraphDataModel");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var instanceRepository = scope.ServiceProvider.GetRequiredService<IInstanceRepository>();
            var completenessCheckScheduler = scope.ServiceProvider.GetRequiredService<ICompletenessCheckScheduler>();

            await ProcessGraphDataModel(graphDataModel, instanceRepository, completenessCheckScheduler);

            graphDataModel.GraphMetadata.ForEach(n =>
                _logger.LogInformation("Processed Instance with Id: {InstanceId}", n.Id)
            );

            _logger.LogInformation("Successfully processed GraphDataModel message");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message as JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message");
        }
    }

    private async Task ProcessGraphDataModel(GraphDataModel graphDataModel, IInstanceRepository instanceRepository, ICompletenessCheckScheduler completenessCheckScheduler)
    {
        var idMap = await ApplyInstancesFromMetadata(graphDataModel, instanceRepository);
        await CreateRelationsFromGraphData(graphDataModel, instanceRepository, idMap);
        completenessCheckScheduler.Schedule(idMap.Values);
    }

    /// <summary>
    /// Creates new instances or updates existing ones based on the metadata provided in the graph data model.
    /// </summary>
    /// <param name="graphDataModel">The graph data model containing the metadata.</param>
    /// <param name="instanceRepository">The repository for instance data operations.</param>
    /// <returns>A dictionary mapping original GUIDs to the new or existing instance IDs.</returns>
    private async Task<Dictionary<string, string>> ApplyInstancesFromMetadata(GraphDataModel graphDataModel, IInstanceRepository instanceRepository)
    {
        if (graphDataModel.GraphMetadata.IsNullOrEmpty())
        {
            _logger.LogInformation("MetaData does not contain any nodes.");
            return new Dictionary<string, string>();
        }

        var building = graphDataModel.GraphMetadata.FirstOrDefault(g => g.ClassType.Contains("Building"));
        var name = building?.PropertiesValues?.FirstOrDefault(p => p.Key == "Name").Value ?? "Default Building Name";

        _logger.LogInformation("Start upserting {Count} Instances.", graphDataModel.GraphMetadata.Count());

        return await instanceRepository.UpsertInstances(graphDataModel.GraphMetadata, name);
    }

    /// <summary>
    /// Creates relations between instances based on the RDF graph data.
    /// </summary>
    /// <param name="graphDataModel">The graph data model containing the graph data.</param>
    /// <param name="instanceRepository">The repository for instance data operations.</param>
    /// <param name="idMap">A dictionary mapping original GUIDs to instance IDs.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task CreateRelationsFromGraphData(GraphDataModel graphDataModel, IInstanceRepository instanceRepository, Dictionary<string, string> idMap)
    {
        if (string.IsNullOrEmpty(graphDataModel.GraphData)) return;

        try
        {
            IGraph graph = new Graph();
            graph.LoadFromString(graphDataModel.GraphData, new TurtleParser());

            var relations = ExtractRelationsFromGraph(graph, idMap);

            if (relations.Count > 0)
            {
                await instanceRepository.CreateRelations(relations.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing RDF graph data");
        }
    }

    /// <summary>
    /// Extracts subject-object-predicate relations from an RDF graph.
    /// </summary>
    /// <param name="graph">The RDF graph to parse.</param>
    /// <param name="idMap">A dictionary mapping original GUIDs to instance IDs.</param>
    /// <returns>A list of tuples representing the relations.</returns>
    private List<(string subjectId, string objectId, string predicateUri)> ExtractRelationsFromGraph(IGraph graph, Dictionary<string, string> idMap)
    {
        var relations = new List<(string subjectId, string objectId, string predicateUri)>();

        foreach (Triple triple in graph.Triples)
        {
            if (triple.Subject is IUriNode subjectNode && triple.Object is IUriNode objectNode)
            {
                var subjectId = idMap.FirstOrDefault(x => subjectNode.Uri.ToString().Contains(x.Key)).Value;
                var objectId = idMap.FirstOrDefault(x => objectNode.Uri.ToString().Contains(x.Key)).Value;

                if (!string.IsNullOrEmpty(subjectId) && !string.IsNullOrEmpty(objectId))
                {
                    _logger.LogInformation(
                        "Creating relation between {SubjectId} and {ObjectId} with predicate URI {PredicateUri}",
                        subjectId, objectId, triple.Predicate.ToString()
                    );
                    relations.Add((subjectId, objectId, triple.Predicate.ToString()));
                }
            }
        }

        return relations;
    }
}
