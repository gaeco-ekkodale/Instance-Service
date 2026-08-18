// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;
using Gremlin.Net.Process.Traversal;

namespace InstanceService.Api.Utilities;

/// <summary>
/// Executes graph queries via Gremlin for instance completeness checks.
/// Replaces the previous CypherQueryExecutor.
///
/// Comparison:
/// - CypherQueryExecutor injected IGraphClient (Neo4j) and built Cypher strings
/// - GremlinGraphQueryExecutor injects GraphTraversalSource (Gremlin) and uses the bytecode API
/// </summary>
public class GremlinGraphQueryExecutor : IGraphQueryExecutor
{
    private readonly GraphTraversalSource _g;
    private readonly IInstanceRepository _instanceRepository;
    private readonly ILogger<GremlinGraphQueryExecutor> _logger;

    public GremlinGraphQueryExecutor(
        GraphTraversalSource g,
        IInstanceRepository instanceRepository,
        ILogger<GremlinGraphQueryExecutor> logger)
    {
        _g = g;
        _instanceRepository = instanceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executes a completeness query to find all related instances for a given instance ID.
    ///
    /// Neo4j/Cypher equivalent:
    /// MATCH path = (start:Instance {Id: $instanceId})-[*0..255]-(related:Instance)
    /// WHERE ALL(n IN nodes(path) WHERE n.ClassificationId IN $relevantClasses OR n.Id = $instanceId)
    /// RETURN related
    ///
    /// Gremlin explanation:
    /// - V().Has("Instance", "Id", id) = Find the start vertex with this ID
    /// - .Repeat(.Both().HasLabel("Instance").SimplePath()) = Repeatedly traverse edges to
    /// Instance vertices, but do not visit a vertex twice (SimplePath prevents cycles)
    /// - .Until(.Loops().Is(P.Gte(255))) = Stop after at most 255 steps (protection against infinite loops)
    /// - .Emit() = Emit every visited vertex (not only the last)
    /// - .Has("ClassificationId", P.Within(...)) = Filter by relevant ClassificationIds
    /// - .Dedup() = Remove duplicates
    /// </summary>
    public async Task<IEnumerable<Instance>> ExecuteCompletenessQueryAsync(
        string instanceId,
        List<string> relevantClasses)
    {
        try
        {
            // Step 1: Find all connected instances in the graph via Gremlin
            var connectedIds = await _g.V()
                .Has("Instance", "Id", instanceId)
                .Repeat(__.Both().HasLabel("Instance").SimplePath())
                .Until(__.Loops().Is(P.Gte(255)))
                .Emit()
                .Has("ClassificationId", P.Within(relevantClasses.Cast<object>().ToArray()))
                .Dedup()
                .Values<string>("Id")
                .Promise(t => t.ToList());

            // Step 2: Add the start instance itself (if it is relevant)
            var allIds = connectedIds.ToHashSet();
            allIds.Add(instanceId);

            // Step 3: Retrieve the full instance data (including metadata from PostgreSQL)
            var results = new List<Instance>();
            foreach (var id in allIds)
            {
                var instance = await _instanceRepository.GetInstance(id);
                if (instance != null)
                    results.Add(instance);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing completeness query for instance {InstanceId}", instanceId);
            return Array.Empty<Instance>();
        }
    }

    /// <summary>
    /// Finds all instances with certain ClassificationIds.
    ///
    /// Neo4j/Cypher equivalent:
    /// MATCH (n:Instance) WHERE n.ClassificationId IN $relevantClasses RETURN n
    ///
    /// Gremlin:
    /// g.V().HasLabel("Instance").Has("ClassificationId", P.Within(...))
    /// P.Within() = "Value is contained in this list" (like SQL IN (...))
    /// </summary>
    public async Task<IEnumerable<Instance>> FindCandidateInstancesAsync(List<string> relevantClasses)
    {
        try
        {
            var vertexResults = await _g.V()
                .HasLabel("Instance")
                .Has("ClassificationId", P.Within(relevantClasses.Cast<object>().ToArray()))
                .ElementMap<object>()
                .Promise(t => t.ToList());

            return vertexResults
                .Where(v => v is not null)
                .Select(v => new Instance
                {
                    Id = GetStringValue(v!, "Id"),
                    Name = GetStringValue(v!, "Name"),
                    ClassificationId = GetStringValue(v!, "ClassificationId"),
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding candidate instances");
            return Array.Empty<Instance>();
        }
    }

    /// <summary>
    /// Helper method: reads a string value from an ElementMap-style dictionary.
    /// Returns the string for the given key, or an empty string if the key is missing or the value is null.
    /// </summary>
    private static string GetStringValue(IDictionary<object, object> dict, string key)
    {
        return dict.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : string.Empty;
    }
}