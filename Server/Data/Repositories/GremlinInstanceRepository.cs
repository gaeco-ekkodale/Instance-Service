// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Data.Exceptions;
using InstanceService.Domain.IRepositories;
using InstanceService.Models;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InstanceService.Data.Repositories;

/// <summary>
/// Implementation of <see cref="IInstanceRepository"/> that uses ArcadeDB via native Gremlin (port 8182).
/// All graph operations are performed through the Gremlin.Net bytecode API (op=bytecode) using a
/// <see cref="GraphTraversalSource"/>; metadata is persisted to PostgreSQL.
///
/// Comparison with InstanceRepository.cs (Neo4j version):
/// - Instead of graphClient.Cypher.Match(...) → g.V().Has(...)
/// - Instead of graphClient.Tx.BeginTransaction() → no explicit transaction management required
/// - Instead of MATCH (n:Instance) WHERE n.Id = $id → g.V().Has("Instance", "Id", id)
/// - The PostgreSQL part (dbContext) is identical
/// </summary>
public class GremlinInstanceRepository(GraphTraversalSource g, InstanceServiceDbContext dbContext) : IInstanceRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// Neo4j equivalent: InstanceQueryElement.Get(graphClient)
    /// Gremlin: g.V().HasLabel("Instance") = "Get all vertices with label Instance"
    /// .ElementMap() = "Return all properties as a dictionary"
    /// </remarks>
    public async Task<IEnumerable<Instance>> GetInstances(bool withMetadata = false)
    {
        var vertexResults = await g.V().HasLabel("Instance")
            .ElementMap<object>()
            .Promise(t => t.ToList());

        var instances = vertexResults
            .Where(v => v is not null)
            .Select(v => MapVertexToInstance(v!))
            .ToList();

        // Load all edges between Instance vertices in a single bulk query
        var edgeResults = await g.V().HasLabel("Instance")
            .BothE()
            .Project<object>("SubjectId", "PredicateUri", "ObjectId")
            .By(__.OutV().Values<object>("Id"))
            .By(T.Label)
            .By(__.InV().Values<object>("Id"))
            .Promise(t => t.ToList());

        var allRelations = edgeResults
            .Where(e => e is not null)
            .Select(e => MapEdgeToRelation(e!))
            .ToList();

        foreach (var instance in instances)
        {
            instance.Relations = allRelations
                .Where(r => r.SubjectId == instance.Id || r.ObjectId == instance.Id)
                .ToList();
        }

        if (withMetadata)
        {
            foreach (var instance in instances)
            {
                var metadata = await dbContext.InstanceMetadata.FindAsync(instance.Id)
                    ?? throw new DatabaseException($"Inconsistent data for the instance with id ({instance.Id}).");

                instance.Properties = metadata.Properties;
            }
        }

        return instances;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neo4j equivalent: InstanceQueryElement.Get(graphClient, id)
    /// Gremlin: g.V().Has("Instance", "Id", id) = "Find the vertex with label Instance and property Id = id"
    ///
    /// For relations:
    /// Neo4j: MATCH (n:Instance)-[r]-(m) WHERE n.Id = $id RETURN r
    /// Gremlin: g.V().Has("Instance", "Id", id).BothE().Project(...)
    /// </remarks>
    public async Task<Instance?> GetInstance(string id)
    {
        var vertexResults = await g.V().Has("Instance", "Id", id)
            .ElementMap<object>()
            .Promise(t => t.ToList());

        var instance = vertexResults.Where(v => v is not null).Select(v => MapVertexToInstance(v!)).SingleOrDefault();

        var metadata = await dbContext.InstanceMetadata.FindAsync(id);

        // When both null the node does not exist — valid case, return null
        if (instance == null && metadata == null)
            return null;

        // Only one source has data — inconsistent state
        if (instance == null ^ metadata == null)
            throw new DatabaseException($"Inconsistent data for the instance with id ({id}).");

        // Load relations: BothE() fetches all edges (incoming and and outgoing)
        var relationResults = await g.V().Has("Instance", "Id", id)
            .BothE()
            .Project<object>("SubjectId", "PredicateUri", "ObjectId")
            .By(__.OutV().Values<object>("Id"))
            .By(T.Label)
            .By(__.InV().Values<object>("Id"))
            .Promise(t => t.ToList());

        instance!.Relations = relationResults
            .Where(e => e is not null)
            .Select(e => MapEdgeToRelation(e!))
            .ToList();

        instance.Properties = metadata!.Properties;

        return instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neo4j equivalent:
    /// graphClient.Tx.Cypher.Create("(instance:Instance)")
    /// .Set("instance = $instance").WithParam("instance", new { ... })
    ///
    /// Gremlin:
    /// g.AddV("Instance").Property("Id", id).Property("Name", name)...
    /// AddV = "Add Vertex"
    /// .Property() = set a property
    ///
    /// Note: Neo4j provides explicit transactions (BeginTransaction/Commit/Rollback).
    /// Gremlin via ArcadeDB does not expose explicit transaction management through the TinkerPop API.
    /// Individual traversals are atomic.
    /// </remarks>
    public async Task CreateInstance(string name, string classificationId, Dictionary<string, string> data, string id)
    {
        var vertexCreated = false;
        try
        {
            await g.AddV("Instance")
                .Property("Id", id)
                .Property("Name", name)
                .Property("ClassificationId", classificationId)
                .Promise(t => t.Iterate());
            vertexCreated = true;

            dbContext.Add(new InstanceMetaData
            {
                Id = id,
                Name = name,
                ClassificationId = classificationId,
                Properties = data,
            });
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            if (vertexCreated)
            {
                // Compensating action: remove the graph vertex so the two stores stay in sync.
                try { await g.V().Has("Instance", "Id", id).Drop().Promise(t => t.Iterate()); }
                catch { /* best-effort; inconsistency will be surfaced on next read */ }
            }
            throw new DatabaseException("Something went wrong in a database interaction. The graph change has been rolled back.", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a new instance with a generated Id.
    /// Generates a new GUID for the instance Id, delegates to CreateInstance(name, classificationId, data, id),
    /// and returns the generated Id.
    /// </remarks>
    public async Task<string> CreateInstance(string name, string classificationId, Dictionary<string, string> data)
    {
        var id = Guid.NewGuid().ToString();
        await CreateInstance(name, classificationId, data, id);
        return id;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neo4j equivalent:
    /// graphClient.Tx.Cypher.Match("(instance:Instance)")
    /// .Where(instance => instance.Id == id)
    /// .Set("instance.Name = $name")
    ///
    /// Gremlin:
    /// g.V().Has("Instance", "Id", id).Property("Name", name)
    /// = "Find the vertex and set the Name property to the new value"
    /// </remarks>
    public async Task UpdateInstance(string id, string name, Dictionary<string, string> data)
    {
        string? previousName = null;
        var graphUpdated = false;
        try
        {
            var instanceMetadata = await dbContext.InstanceMetadata.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(typeof(InstanceMetaData), id);

            previousName = instanceMetadata.Name;

            await g.V().Has("Instance", "Id", id)
                .Property("Name", name)
                .Promise(t => t.Iterate());
            graphUpdated = true;

            var newProperties = new Dictionary<string, string>(instanceMetadata.Properties);
            foreach (var property in data)
                newProperties[property.Key] = property.Value;

            instanceMetadata.Properties = newProperties;
            instanceMetadata.Name = name;

            await dbContext.SaveChangesAsync();
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (graphUpdated && previousName != null)
            {
                // Compensating action: restore the previous name in the graph.
                try { await g.V().Has("Instance", "Id", id).Property("Name", previousName).Promise(t => t.Iterate()); }
                catch { /* best-effort; inconsistency will be surfaced on next read */ }
            }
            throw new DatabaseException("Something went wrong in a database interaction. The graph change has been rolled back.", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neo4j equivalent:
    /// graphClient.Cypher.Match("(instance:Instance)")
    /// .Where(instance => instance.Id == id).DetachDelete("instance")
    /// DetachDelete = delete the node and all its edges
    ///
    /// Gremlin:
    /// g.V().Has("Instance", "Id", id).Drop()
    /// Drop() deletes the vertex AND all connected edges automatically.
    /// </remarks>
    public async Task DeleteInstance(string id)
    {
        var vertexResults = await g.V().Has("Instance", "Id", id)
            .ElementMap<object>()
            .Promise(t => t.ToList());

        if (!vertexResults.Any())
            throw new NotFoundException(typeof(Instance), id);

        var metadata = dbContext.InstanceMetadata.FirstOrDefault(i => i.Id == id);
        var metadataDeleted = false;
        try
        {
            // Delete relational row first (cheaper to retry/roll back)
            if (metadata != null)
            {
                dbContext.InstanceMetadata.Remove(metadata);
                await dbContext.SaveChangesAsync();
                metadataDeleted = true;
            }

            // Drop the graph vertex and all connected edges
            await g.V().Has("Instance", "Id", id)
                .Drop()
                .Promise(t => t.Iterate());
        }
        catch (Exception ex)
        {
            if (metadataDeleted && metadata != null)
            {
                // Compensating action: restore the deleted PG row so the two stores stay in sync.
                try { dbContext.InstanceMetadata.Add(metadata); await dbContext.SaveChangesAsync(); }
                catch { /* best-effort; inconsistency will be surfaced on next read */ }
            }
            throw new DatabaseException("Failed to delete instance. Changes have been rolled back.", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Deletes instances by classification IDs. Removes PostgreSQL metadata rows first (cheaper to retry/roll back),
    /// then drops the graph vertices. If graph deletion fails, compensates by restoring the deleted PG rows.
    /// Returns the count of deleted instances from PostgreSQL (which can diverge from actual dropped vertices
    /// if graph deletion partially fails).
    /// </remarks>
    public async Task<int> DeleteInstancesByClassificationIds(IEnumerable<string> classificationIds)
    {
        var ids = classificationIds?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return 0;

        var metadata = await dbContext.InstanceMetadata
            .Where(m => ids.Contains(m.ClassificationId))
            .ToListAsync();

        if (metadata.Count == 0)
            return 0;

        var rowsDeleted = 0;
        try
        {
            // Delete relational rows first (cheaper to retry/roll back)
            dbContext.InstanceMetadata.RemoveRange(metadata);
            await dbContext.SaveChangesAsync();
            rowsDeleted = metadata.Count;

            // Drop the graph vertices; Drop() removes the vertex together with all connected edges.
            // P.Within matches any vertex whose ClassificationId is one of the removed classifications.
            await g.V().Has("Instance", "ClassificationId", P.Within(ids.Cast<object>().ToArray()))
                .Drop()
                .Promise(t => t.Iterate());
        }
        catch (Exception ex)
        {
            if (rowsDeleted > 0)
            {
                // Compensating action: restore the deleted PG rows so the two stores stay in sync.
                try { dbContext.InstanceMetadata.AddRange(metadata); await dbContext.SaveChangesAsync(); }
                catch { /* best-effort; inconsistency will be surfaced on next read */ }
            }
            throw new DatabaseException("Failed to delete instances by classification IDs. Changes have been rolled back.", ex);
        }

        return rowsDeleted;
    }

    /// <remarks>
    /// Neo4j equivalent:
    /// MATCH (instance:Instance)-[r]-() WHERE instance.Id = $id DELETE r
    ///
    /// Gremlin:
    /// g.V().Has("Instance", "Id", id).BothE().Drop()
    /// BothE() = all edges (incoming and outgoing), Drop() = delete
    /// </remarks>
    public async Task DeleteRelations(string id)
    {
        var vertexResults = await g.V().Has("Instance", "Id", id)
            .ElementMap<object>()
            .Promise(t => t.ToList());

        if (!vertexResults.Any())
            throw new NotFoundException(typeof(Instance), id);

        await g.V().Has("Instance", "Id", id)
            .BothE()
            .Drop()
            .Promise(t => t.Iterate());
    }

    /// <remarks>
    /// Neo4j equivalent:
    /// MATCH (subject:Instance)-[r]->(object:Instance)
    /// WHERE subject.Id = $subjectId AND object.Id = $objectId DELETE r
    ///
    /// Gremlin:
    /// g.V().Has("Instance", "Id", subjectId)
    /// .OutE(predicateUri) = outgoing edges of that predicate type only

    /// .Where(__.InV().Has("Instance", "Id", objectId)) = those that lead to the target vertex
    /// .Drop() = delete
    /// </remarks>
    public async Task DeleteRelation(string subjectId, string objectId, string predicateUri)
    {
        await g.V().Has("Instance", "Id", subjectId)
            .OutE(predicateUri)
            .Where(__.InV().Has("Instance", "Id", objectId))
            .Drop()
            .Promise(t => t.Iterate());
    }

    /// <inheritdoc />
    /// <remarks>
    /// Neo4j equivalent (uses the APOC plugin):
    /// UNWIND relations AS relation
    /// MATCH (subject), (object) WHERE ...
    /// CALL apoc.merge.relationship(subject, relation.PredicateUri, ...)
    ///
    /// Gremlin:
    /// Coalesce = "Try X first; if not found do Y" (= upsert / idempotent)
    /// - First check if the edge already exists (InE)
    /// - If not, create a new edge (AddE)
    /// AddE = "Add Edge"
    /// </remarks>
    public async Task CreateRelations(params InstanceRelation[] relations)
    {
        foreach (var relation in relations)
        {
            // coalesce: reuse existing edge if present, otherwise create new one (idempotent upsert)
            await g.V().Has("Instance", "Id", relation.SubjectId).As("s")
                .V().Has("Instance", "Id", relation.ObjectId)
                .Coalesce<Edge>(
                    __.InE(relation.PredicateUri).Where(__.OutV().As("s")),
                    __.AddE(relation.PredicateUri).From("s"))
                .Promise(t => t.Iterate());
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The logic is identical to the Neo4j version; only the graph calls use Gremlin instead of Cypher.
    /// </remarks>
    public async Task<Dictionary<string, string>> UpsertInstances(IEnumerable<MetaDataNode> newInstances, string name)
    {
        var idMap = new Dictionary<string, string>();

        if (newInstances.IsNullOrEmpty())
            return idMap;

        foreach (var instance in newInstances)
        {
            var instanceId = instance.Id;
            var existingMetadata = await dbContext.InstanceMetadata.FindAsync(instance.Id);

            if (existingMetadata != null)
            {
                var vertexCount = await g.V().Has("Instance", "Id", instanceId)
                    .Count()
                    .Promise(t => t.Next());

                if (vertexCount == 0)
                {
                    // Graph node is missing despite the PG row being present — inconsistent state.
                    // Restore the graph node without touching PG.
                    await g.AddV("Instance")
                        .Property("Id", instanceId)
                        .Property("Name", existingMetadata.Name)
                        .Property("ClassificationId", existingMetadata.ClassificationId)
                        .Promise(t => t.Iterate());
                }
                else
                {
                    await UpdateInstance(instanceId, existingMetadata.Name, instance.PropertiesValues ?? []);
                }
            }
            else
            {
                var dataDict = instance.PropertiesValues?.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "")
                              ?? new Dictionary<string, string>();

                if (!dataDict.ContainsKey("Name"))
                    dataDict["Name"] = name;

                await CreateInstance(
                    $"{instance.Code} {dataDict["Name"] ?? name}",
                    instance.ClassType,
                    dataDict,
                    instance.Id);
            }

            idMap[instance.Id] = instanceId;
        }

        return idMap;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a vertex ElementMap result to an <see cref="Instance"/>.
    ///
    /// ElementMap returns a dictionary where keys are either system keys (T.Id, T.Label)
    /// or plain strings ("Name", "Id"). We only read the string keys (our own properties).
    /// </summary>
    private static Instance MapVertexToInstance(IDictionary<object, object> vertex)
    {
        return new Instance
        {
            Id = GetStringValue(vertex, "Id"),
            Name = GetStringValue(vertex, "Name"),
            ClassificationId = GetStringValue(vertex, "ClassificationId"),
        };
    }

    /// <summary>
    /// Maps a Project result (from a BothE projection) to an <see cref="InstanceRelation"/>.
    /// </summary>
    private static InstanceRelation MapEdgeToRelation(IDictionary<string, object> edge)
    {
        return new InstanceRelation
        {
            SubjectId = edge.TryGetValue("SubjectId", out var subj) ? subj?.ToString() ?? string.Empty : string.Empty,
            PredicateUri = edge.TryGetValue("PredicateUri", out var pred) ? pred?.ToString() ?? string.Empty : string.Empty,
            ObjectId = edge.TryGetValue("ObjectId", out var obj) ? obj?.ToString() ?? string.Empty : string.Empty,
        };
    }

    /// <summary>
    /// Helper method: reads a string value from an ElementMap dictionary.
    /// </summary>
    private static string GetStringValue(IDictionary<object, object> dict, string key)
    {
        return dict.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : string.Empty;
    }
}