// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentAssertions;
using InstanceService.Api.Utilities;
using InstanceService.Models;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace InstanceService.Api.Tests.Utilities;

public class GraphProcessingServiceTests
{

    [Fact]
    public void GenerateRelationsFromMetadata_WithValidData_ShouldGenerateCorrectTriples()
    {
        // Arrange
        var metadataNodes = new List<MetaDataNode>
        {
            new MetaDataNode
            {
                Id = "instance1",
                ClassType = "http://example.org/ontology#Wall"
            },
            new MetaDataNode
            {
                Id = "instance2", 
                ClassType = "http://example.org/ontology#Room"
            }
        };

        var ontologyGraph = new Graph();
        var subjectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Wall"));
        var predicateNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#contains"));
        var objectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Room"));
        
        var ontologyRelations = new List<Triple>
        {
            new Triple(subjectNode, predicateNode, objectNode)
        };

        // Act
        var result = GraphProcessingService.GenerateRelationsFromMetadata(metadataNodes, ontologyRelations);

        // Assert
        result.Should().HaveCount(1);
        var triple = result[0];
        
        // Verify subject URI contains instance1
        ((IUriNode)triple.Subject).Uri.ToString().Should().Contain("instance1");
        
        // Verify predicate is preserved from ontology
        triple.Predicate.Should().Be(predicateNode);
        
        // Verify object URI contains instance2
        ((IUriNode)triple.Object).Uri.ToString().Should().Contain("instance2");
    }

    [Fact]
    public void GenerateRelationsFromMetadata_WithCustomBaseNamespace_ShouldUseCustomNamespace()
    {
        // Arrange
        var customNamespace = "http://custom.org/instances/";
        var metadataNodes = new List<MetaDataNode>
        {
            new MetaDataNode
            {
                Id = "wall1",
                ClassType = "http://example.org/ontology#Wall"
            },
            new MetaDataNode
            {
                Id = "room1",
                ClassType = "http://example.org/ontology#Room"
            }
        };

        var ontologyGraph = new Graph();
        var subjectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Wall"));
        var predicateNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#isPartOf"));
        var objectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Room"));
        
        var ontologyRelations = new List<Triple>
        {
            new Triple(subjectNode, predicateNode, objectNode)
        };

        // Act
        var result = GraphProcessingService.GenerateRelationsFromMetadata(metadataNodes, ontologyRelations, customNamespace);

        // Assert
        result.Should().HaveCount(1);
        var triple = result.First();
        
        ((IUriNode)triple.Subject).Uri.ToString().Should().StartWith(customNamespace);
        ((IUriNode)triple.Object).Uri.ToString().Should().StartWith(customNamespace);
    }

    [Fact]
    public void GenerateRelationsFromMetadata_WithNoMatchingOntologyRules_ShouldReturnEmptyList()
    {
        // Arrange
        var metadataNodes = new List<MetaDataNode>
        {
            new MetaDataNode
            {
                Id = "instance1",
                ClassType = "http://example.org/ontology#Door"
            }
        };

        var ontologyGraph = new Graph();
        var subjectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Wall"));
        var predicateNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#contains"));
        var objectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Room"));
        
        var ontologyRelations = new List<Triple>
        {
            new Triple(subjectNode, predicateNode, objectNode)
        };

        // Act
        var result = GraphProcessingService.GenerateRelationsFromMetadata(metadataNodes, ontologyRelations);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateRelationsFromMetadata_WithMissingTargetObject_ShouldNotGenerateTriple()
    {
        // Arrange
        var metadataNodes = new List<MetaDataNode>
        {
            new MetaDataNode
            {
                Id = "instance1",
                ClassType = "http://example.org/ontology#Wall"
            }
            // Missing Room instance
        };

        var ontologyGraph = new Graph();
        var subjectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Wall"));
        var predicateNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#contains"));
        var objectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Room"));
        
        var ontologyRelations = new List<Triple>
        {
            new Triple(subjectNode, predicateNode, objectNode)
        };

        // Act
        var result = GraphProcessingService.GenerateRelationsFromMetadata(metadataNodes, ontologyRelations);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertRelationsToTurtle_WithValidTriples_ShouldGenerateValidTurtleString()
    {
        // Arrange
        var ontologyGraph = new Graph();
        ontologyGraph.NamespaceMap.AddNamespace("ex", new Uri("http://example.org/ontology#"));
        ontologyGraph.NamespaceMap.AddNamespace("inst", new Uri("http://example.org/instances/"));

        var subjectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/wall1"));
        var predicateNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#contains"));
        var objectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/room1"));

        var relations = new List<Triple>
        {
            new Triple(subjectNode, predicateNode, objectNode)
        };

        // Act
        var turtleString = GraphProcessingService.ConvertRelationsToTurtle(relations, ontologyGraph);

        // Assert
        turtleString.Should().NotBeNullOrEmpty();
        
        // Verify the turtle string contains namespace declarations
        turtleString.Should().Contain("@prefix");
        
        // Verify the turtle string contains the relation
        turtleString.Should().Contain("wall1");
        turtleString.Should().Contain("room1");
        turtleString.Should().Contain("contains");

        // Verify it's valid turtle by parsing it back
        var parser = new TurtleParser();
        var testGraph = new Graph();
        Action parseAction = () => parser.Load(testGraph, new StringReader(turtleString));
        parseAction.Should().NotThrow();
        
        // Verify the parsed graph contains our triple
        testGraph.Triples.Should().HaveCount(1);
    }

    [Fact]
    public void ConvertRelationsToTurtle_WithMultipleTriples_ShouldGenerateCorrectTurtleString()
    {
        // Arrange
        var ontologyGraph = new Graph();
        ontologyGraph.NamespaceMap.AddNamespace("ex", new Uri("http://example.org/ontology#"));
        ontologyGraph.NamespaceMap.AddNamespace("inst", new Uri("http://example.org/instances/"));

        var wall1 = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/wall1"));
        var wall2 = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/wall2"));
        var room1 = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/room1"));
        var contains = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#contains"));
        var adjacentTo = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#adjacentTo"));

        var relations = new List<Triple>
        {
            new Triple(wall1, contains, room1),
            new Triple(wall1, adjacentTo, wall2)
        };

        // Act
        var turtleString = GraphProcessingService.ConvertRelationsToTurtle(relations, ontologyGraph);

        // Assert
        turtleString.Should().NotBeNullOrEmpty();
        
        // Parse back to verify structure
        var parser = new TurtleParser();
        var testGraph = new Graph();
        parser.Load(testGraph, new StringReader(turtleString));
        
        testGraph.Triples.Should().HaveCount(2);
        
        // Verify specific triples exist
        var containsTriple = testGraph.Triples.FirstOrDefault(t => 
            ((IUriNode)t.Subject).Uri.ToString().Contains("wall1") &&
            ((IUriNode)t.Predicate).Uri.ToString().Contains("contains") &&
            ((IUriNode)t.Object).Uri.ToString().Contains("room1"));
        containsTriple.Should().NotBeNull();

        var adjacentTriple = testGraph.Triples.FirstOrDefault(t => 
            ((IUriNode)t.Subject).Uri.ToString().Contains("wall1") &&
            ((IUriNode)t.Predicate).Uri.ToString().Contains("adjacentTo") &&
            ((IUriNode)t.Object).Uri.ToString().Contains("wall2"));
        adjacentTriple.Should().NotBeNull();
    }

    [Fact]
    public void ConvertRelationsToTurtle_ShouldPreserveNamespaceMappings()
    {
        // Arrange
        var ontologyGraph = new Graph();
        ontologyGraph.NamespaceMap.AddNamespace("building", new Uri("http://building.org/ontology#"));
        ontologyGraph.NamespaceMap.AddNamespace("spatial", new Uri("http://spatial.org/ontology#"));

        var subjectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/wall1"));
        var predicateNode = ontologyGraph.CreateUriNode(new Uri("http://building.org/ontology#isPartOf"));
        var objectNode = ontologyGraph.CreateUriNode(new Uri("http://example.org/instances/room1"));

        var relations = new List<Triple>
        {
            new Triple(subjectNode, predicateNode, objectNode)
        };

        // Act
        var turtleString = GraphProcessingService.ConvertRelationsToTurtle(relations, ontologyGraph);

        // Assert
        turtleString.Should().Contain("@prefix building:");
        turtleString.Should().Contain("@prefix spatial:");
        turtleString.Should().Contain("@prefix inst:");
        
        // Verify namespace URIs are preserved
        turtleString.Should().Contain("http://building.org/ontology#");
        turtleString.Should().Contain("http://spatial.org/ontology#");
    }

    [Fact]
    public void ConvertRelationsToTurtle_WithEmptyRelations_ShouldGenerateValidTurtleWithOnlyNamespaces()
    {
        // Arrange
        var ontologyGraph = new Graph();
        ontologyGraph.NamespaceMap.AddNamespace("ex", new Uri("http://example.org/ontology#"));

        var relations = new List<Triple>();

        // Act
        var turtleString = GraphProcessingService.ConvertRelationsToTurtle(relations, ontologyGraph);

        // Assert
        turtleString.Should().NotBeNullOrEmpty();
        turtleString.Should().Contain("@prefix ex:");
        turtleString.Should().Contain("@prefix inst:");
        
        // Should be parseable even with no triples
        var parser = new TurtleParser();
        var testGraph = new Graph();
        Action parseAction = () => parser.Load(testGraph, new StringReader(turtleString));
        parseAction.Should().NotThrow();
        
        testGraph.Triples.Should().BeEmpty();
    }

    [Fact]
    public void EndToEndTest_GenerateRelationsAndConvertToTurtle_ShouldProduceCorrectResult()
    {
        // Arrange - Create a realistic scenario with building elements
        var metadataNodes = new List<MetaDataNode>
        {
            new MetaDataNode
            {
                Id = "wall-001",
                ClassType = "http://buildingsmart.org/ontology#Wall"
            },
            new MetaDataNode
            {
                Id = "room-101",
                ClassType = "http://buildingsmart.org/ontology#Space"
            },
            new MetaDataNode
            {
                Id = "door-001",
                ClassType = "http://buildingsmart.org/ontology#Door"
            }
        };

        // Create ontology with relationships
        var ontologyGraph = new Graph();
        ontologyGraph.NamespaceMap.AddNamespace("ifc", new Uri("http://buildingsmart.org/ontology#"));
        ontologyGraph.NamespaceMap.AddNamespace("rel", new Uri("http://buildingsmart.org/relations#"));

        var wallClass = ontologyGraph.CreateUriNode(new Uri("http://buildingsmart.org/ontology#Wall"));
        var spaceClass = ontologyGraph.CreateUriNode(new Uri("http://buildingsmart.org/ontology#Space"));
        var doorClass = ontologyGraph.CreateUriNode(new Uri("http://buildingsmart.org/ontology#Door"));
        var boundsRelation = ontologyGraph.CreateUriNode(new Uri("http://buildingsmart.org/relations#bounds"));
        var providesAccessRelation = ontologyGraph.CreateUriNode(new Uri("http://buildingsmart.org/relations#providesAccessTo"));

        var ontologyRelations = new List<Triple>
        {
            new Triple(wallClass, boundsRelation, spaceClass),
            new Triple(doorClass, providesAccessRelation, spaceClass)
        };

        // Act
        var generatedRelations = GraphProcessingService.GenerateRelationsFromMetadata(metadataNodes, ontologyRelations);
        var turtleString = GraphProcessingService.ConvertRelationsToTurtle(generatedRelations, ontologyGraph);

        // Assert
        generatedRelations.Should().HaveCount(2);
        
        turtleString.Should().NotBeNullOrEmpty();
        turtleString.Should().Contain("wall-001");
        turtleString.Should().Contain("room-101");
        turtleString.Should().Contain("door-001");
        turtleString.Should().Contain("bounds");
        turtleString.Should().Contain("providesAccessTo");
        
        // Verify namespace declarations
        turtleString.Should().Contain("@prefix ifc:");
        turtleString.Should().Contain("@prefix rel:");
        turtleString.Should().Contain("@prefix inst:");

        // Parse and verify structure
        var parser = new TurtleParser();
        var resultGraph = new Graph();
        parser.Load(resultGraph, new StringReader(turtleString));
        
        resultGraph.Triples.Should().HaveCount(2);
        
        // Verify specific relationships exist
        var wallBoundsSpace = resultGraph.Triples.Any(t =>
            ((IUriNode)t.Subject).Uri.ToString().Contains("wall-001") &&
            ((IUriNode)t.Predicate).Uri.ToString().Contains("bounds") &&
            ((IUriNode)t.Object).Uri.ToString().Contains("room-101"));
        wallBoundsSpace.Should().BeTrue("Wall should bound the space");

        var doorProvidesAccess = resultGraph.Triples.Any(t =>
            ((IUriNode)t.Subject).Uri.ToString().Contains("door-001") &&
            ((IUriNode)t.Predicate).Uri.ToString().Contains("providesAccessTo") &&
            ((IUriNode)t.Object).Uri.ToString().Contains("room-101"));
        doorProvidesAccess.Should().BeTrue("Door should provide access to the space");
    }

    [Theory]
    [InlineData("http://custom.namespace.org/instances/")]
    [InlineData("https://mycompany.com/building/instances/")]
    [InlineData("http://example.org/project123/instances/")]
    public void GenerateRelationsFromMetadata_WithDifferentNamespaces_ShouldRespectCustomNamespace(string customNamespace)
    {
        // Arrange
        var metadataNodes = new List<MetaDataNode>
        {
            new MetaDataNode { Id = "element1", ClassType = "http://example.org/ontology#Element" },
            new MetaDataNode { Id = "element2", ClassType = "http://example.org/ontology#Container" }
        };

        var ontologyGraph = new Graph();
        var elementClass = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Element"));
        var containerClass = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#Container"));
        var containedBy = ontologyGraph.CreateUriNode(new Uri("http://example.org/ontology#containedBy"));

        var ontologyRelations = new List<Triple>
        {
            new Triple(elementClass, containedBy, containerClass)
        };

        // Act
        var result = GraphProcessingService.GenerateRelationsFromMetadata(metadataNodes, ontologyRelations, customNamespace);

        // Assert
        result.Should().HaveCount(1);
        var triple = result.First();
        
        ((IUriNode)triple.Subject).Uri.ToString().Should().StartWith(customNamespace);
        ((IUriNode)triple.Object).Uri.ToString().Should().StartWith(customNamespace);
        ((IUriNode)triple.Subject).Uri.ToString().Should().EndWith("element1");
        ((IUriNode)triple.Object).Uri.ToString().Should().EndWith("element2");
    }
}