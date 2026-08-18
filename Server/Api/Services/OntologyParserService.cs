// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Data;
using InstanceService.Models.Ontology;
using Microsoft.EntityFrameworkCore;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace InstanceService.Api.Services;

public interface IOntologyParserService
{
    /// <summary>
    /// Parses the TTL/RDF file and stores it as the current relational projection for the ontology.
    /// Replace-on-update: any previously stored version for the same <paramref name="ontologyId"/> is
    /// removed before the new one is inserted, so only the latest version is ever kept. A re-delivered
    /// file with an already-stored ETag is a no-op.
    /// </summary>
    Task ParseAndStoreAsync(byte[] ttlData, string ontologyId, string etag, CancellationToken ct = default);

    /// <summary>
    /// Deletes the stored ontology projection (all versions, hierarchies, and relations) for the given
    /// ontology ID. A no-op if nothing is stored. Invoked on a <c>DeletedOntology</c> event.
    /// </summary>
    Task DeleteByOntologyIdAsync(string ontologyId, CancellationToken ct = default);
}

public class OntologyParserService(
    IServiceProvider serviceProvider,
    ILogger<OntologyParserService> logger) : IOntologyParserService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<OntologyParserService> _logger = logger;

    private const string HierarchyQuery = """
        PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
        SELECT DISTINCT ?child ?parent
        WHERE {
            ?child rdfs:subClassOf ?parent .
            FILTER(isIRI(?child) && isIRI(?parent))
        }
        """;

    private const string PropertiesQuery = """
        PREFIX rdf:  <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
        PREFIX owl:  <http://www.w3.org/2002/07/owl#>
        PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
        SELECT DISTINCT ?property ?domain ?range ?label
        WHERE {
            ?property a ?propertyType ;
                     rdfs:domain ?domain ;
                     rdfs:range  ?range .
            OPTIONAL { ?property rdfs:label ?label }
            FILTER(?propertyType = rdf:Property ||
                   ?propertyType = owl:ObjectProperty ||
                   ?propertyType = owl:DatatypeProperty ||
                   ?propertyType = owl:AnnotationProperty)
            FILTER(isIRI(?domain) && isIRI(?range))
        }
        """;

    public async Task ParseAndStoreAsync(byte[] ttlData, string ontologyId, string etag, CancellationToken ct = default)
    {
        var versionId = ParseOntologyId(ontologyId);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

        if (await db.OntologyVersions.AnyAsync(v => v.Id == versionId && v.Etag == etag, ct))
        {
            _logger.LogInformation("Ontology {OntologyId} with Etag {Etag} already stored, skipping parse", ontologyId, etag);
            return;
        }

        _logger.LogInformation("Parsing ontology {OntologyId}", ontologyId);

        var graph = new Graph();
        using (var ms = new MemoryStream(ttlData))
        using (var reader = new StreamReader(ms))
        {
            new TurtleParser().Load(graph, reader);
        }

        var hierarchy = RunQuery(graph, HierarchyQuery)
            .Select(r => (child: r["child"].ToString(), parent: r["parent"].ToString()))
            .Distinct()
            .ToList();

        var relations = RunQuery(graph, PropertiesQuery)
            .Select(r => (
                property: r["property"].ToString(),
                domain: r["domain"].ToString(),
                range: r["range"].ToString(),
                // rdfs:label is a literal — use its lexical Value, not ToString() which would
                // append the datatype suffix (e.g. "...^^http://www.w3.org/2001/XMLSchema#string").
                label: r.HasValue("label") && r["label"] is ILiteralNode label ? label.Value : null))
            .GroupBy(r => (r.property, r.domain, r.range))
            .Select(g => (
                property: g.Key.property,
                domain: g.Key.domain,
                range: g.Key.range,
                label: g.Select(x => x.label).FirstOrDefault(l => l != null) ?? ExtractLocalName(g.Key.property)))
            .ToList();

        _logger.LogInformation("Extracted {HierarchyCount} hierarchy edges, {RelationCount} relations",
            hierarchy.Count, relations.Count);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Replace-on-update: drop the previously stored version for this ontology before inserting
            // the new one. The FK cascade removes the old hierarchy and relation rows.
            await db.OntologyVersions
                .Where(v => v.Id == versionId)
                .ExecuteDeleteAsync(ct);

            var version = new OntologyVersion
            {
                Id = versionId,
                Etag = etag,
                LoadedAt = DateTimeOffset.UtcNow
            };
            db.OntologyVersions.Add(version);
            await db.SaveChangesAsync(ct);

            db.OntologyClassHierarchies.AddRange(hierarchy.Select(h => new OntologyClassHierarchy
            {
                OntologyVersionId = version.Id,
                ChildUri = h.child,
                ParentUri = h.parent
            }));

            db.OntologyRelations.AddRange(relations.Select(r => new OntologyRelation
            {
                OntologyVersionId = version.Id,
                PropertyUri = r.property,
                DomainUri = r.domain,
                RangeUri = r.range,
                Label = r.label
            }));

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        _logger.LogInformation("Ontology {OntologyId} (Etag {Etag}) stored in database", ontologyId, etag);
    }

    public async Task DeleteByOntologyIdAsync(string ontologyId, CancellationToken ct = default)
    {
        var versionId = ParseOntologyId(ontologyId);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

        var removed = await db.OntologyVersions
            .Where(v => v.Id == versionId)
            .ExecuteDeleteAsync(ct);

        if (removed > 0)
            _logger.LogInformation("Deleted ontology {OntologyId} ({VersionCount} version(s)) from database", ontologyId, removed);
        else
            _logger.LogInformation("Delete requested for ontology {OntologyId} but nothing was stored", ontologyId);
    }

    /// <summary>
    /// Parses the ontology id carried by the event (the OntologyService's GUID) into the version primary key.
    /// </summary>
    private static Guid ParseOntologyId(string ontologyId)
    {
        if (Guid.TryParse(ontologyId, out var id))
            return id;
        throw new InvalidOperationException(
            $"Ontology event Id '{ontologyId}' is not a valid GUID; cannot use it as the version primary key.");
    }

    private static string ExtractLocalName(string uri)
    {
        var hashIdx = uri.LastIndexOf('#');
        if (hashIdx >= 0) return uri[(hashIdx + 1)..];
        var slashIdx = uri.LastIndexOf('/');
        return slashIdx >= 0 ? uri[(slashIdx + 1)..] : uri;
    }

    private static IEnumerable<ISparqlResult> RunQuery(IGraph graph, string sparql)
    {
        var query = new SparqlQueryParser().ParseFromString(sparql);

        // Surface an unexpected result type (e.g. a graph result) instead of silently returning
        // an empty set, which would be indistinguishable from "no data" and mask a broken query.
        if (graph.ExecuteQuery(query) is not SparqlResultSet results)
            throw new InvalidOperationException(
                "Expected a SPARQL SELECT result set but the query returned a different result type.");

        return results;
    }
}
