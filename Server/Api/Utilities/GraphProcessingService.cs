// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using VDS.RDF;
using VDS.RDF.Writing;
using InstanceService.Models;

namespace InstanceService.Api.Utilities;

/// <summary>
/// Service for processing graph data and relationships.
/// </summary>
public static class GraphProcessingService
{
    private const string DefaultInstanceNamespace = "http://example.org/instances/";

    /// <inheritdoc />
    public static List<Triple> GenerateRelationsFromMetadata(List<MetaDataNode> metadataNodes, List<Triple> ontologyRelations, string baseNamespace = DefaultInstanceNamespace)
    {
        var generatedRelations = new List<Triple>();
        var graph = new Graph(); // Create a graph for creating nodes

        foreach (var subject in metadataNodes)
        {
            // Filter only ontology rules relevant to this subject
            var relevantRules = ontologyRelations.FindAll(rule => GetUriFromNode(rule.Subject) == subject.ClassType);

            // Process all relevant rules for this subject
            foreach (var rule in relevantRules)
            {
                // Check if target object exists
                var targetObject = metadataNodes.Find(o => o.ClassType == GetUriFromNode(rule.Object));
                if (targetObject != null)
                {
                    INode subjectNode = CreateNodeFromUri(graph, subject.Id, baseNamespace);
                    INode predicateNode = rule.Predicate;
                    INode objectNode = CreateNodeFromUri(graph, targetObject.Id, baseNamespace);

                    generatedRelations.Add(new Triple(subjectNode, predicateNode, objectNode));
                }
            }
        }

        return generatedRelations;
    }

    /// <inheritdoc />
    public static string ConvertRelationsToTurtle(List<Triple> relations, IGraph ontologyGraph)
    {
        // Create a new graph for relations
        IGraph relationGraph = new Graph();

        // Copy namespace mappings from ontology graph
        CopyNamespaceMappings(ontologyGraph, relationGraph);

        // Use HashSet to ensure no duplicate triples are added
        var uniqueTriples = new HashSet<Triple>(relations);

        // Add unique relations to graph
        foreach (var triple in uniqueTriples)
        {
            relationGraph.Assert(triple);
        }

        // Serialize graph to Turtle format
        return SerializeGraphToTurtle(relationGraph);
    }

    /// <summary>
    /// Extracts the URI string from an RDF node.
    /// </summary>
    /// <param name="node">The RDF node to extract the URI from.</param>
    /// <returns>The URI string of the node.</returns>
    private static string GetUriFromNode(INode node)
    {
        if (node is IUriNode uriNode)
        {
            return uriNode.Uri.ToString();
        }
        throw new ArgumentException("Node is not a URI node", nameof(node));
    }

    /// <summary>
    /// Copies all namespace mappings from the source RDF graph to the target RDF graph, including a default namespace for instances.
    /// </summary>
    /// <param name="sourceGraph">The source RDF graph containing namespace mappings (typically the ontology graph).</param>
    /// <param name="targetGraph">The target RDF graph to which namespaces will be copied (typically the relation graph).</param>
    /// <remarks>
    /// This method ensures that all prefixes and namespaces used in the ontology are available in the generated relation graph, so that Turtle serialization produces correct and readable output. It also adds a default namespace for instance data.
    /// </remarks>
    private static void CopyNamespaceMappings(IGraph sourceGraph, IGraph targetGraph)
    {
        foreach (var prefix in sourceGraph.NamespaceMap.Prefixes)
        {
            Uri namespaceUri = sourceGraph.NamespaceMap.GetNamespaceUri(prefix);
            targetGraph.NamespaceMap.AddNamespace(prefix, namespaceUri);
        }
        targetGraph.NamespaceMap.AddNamespace("inst", new Uri(DefaultInstanceNamespace));
    }

    /// <summary>
    /// Creates an RDF node from a URI string, handling both absolute and relative URIs.
    /// </summary>
    /// <param name="graph">The RDF graph in which the node will be created.</param>
    /// <param name="uriString">The URI string to convert to a node. If not absolute, a default namespace is used.</param>
    /// <param name="baseNamespace">The base namespace to use for relative URIs.</param>
    /// <returns>An RDF node representing the given URI.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the URI string is null or empty.</exception>
    /// <remarks>
    /// This method checks if the URI string is absolute. If not, it prepends a default namespace. Used to ensure all nodes in the graph have valid URIs.
    /// </remarks>
    private static INode CreateNodeFromUri(IGraph graph, string uriString, string baseNamespace = DefaultInstanceNamespace)
    {
        if (string.IsNullOrEmpty(uriString))
            throw new ArgumentNullException(nameof(uriString));

        Uri? uri = null;
        if (Uri.TryCreate(uriString, UriKind.Absolute, out var createdUri))
        {
            uri = createdUri;
        }

        if (uri != null)
        {
            return graph.CreateUriNode(uri);
        }        // If not an absolute URI, create URI with provided base namespace
        return graph.CreateUriNode(new Uri(baseNamespace + uriString));
    }

    /// <summary>
    /// Serializes the provided RDF graph to a Turtle (TTL) string, including all namespace mappings and triples.
    /// </summary>
    /// <param name="graph">The RDF graph to serialize.</param>
    /// <returns>A string in Turtle format representing the graph's triples and namespaces.</returns>
    /// <remarks>
    /// This method uses a compressing Turtle writer to produce readable and compact output. Used to generate the final graph data for each building's GraphDataModel.
    /// </remarks>
    private static string SerializeGraphToTurtle(IGraph graph)
    {
        var writer = new CompressingTurtleWriter(0);
        writer.CompressionLevel = 0;
        writer.HighSpeedModePermitted = false;
        var stringWriter = new System.IO.StringWriter();
        writer.Save(graph, stringWriter);

        return stringWriter.ToString();
    }
}