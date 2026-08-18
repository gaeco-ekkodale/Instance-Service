// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using InstanceService.Models.Enum;

namespace InstanceService.Models;

public class GraphDataModel
{
    private string graphTemplate;
    private string graphData;
    private List<MetaDataNode> graphMetadata;
    private List<AccessRight> accessRights;
    private UseCase useCase;
    private object guidelines;

    public GraphDataModel()
    {
        // Initialization
        graphTemplate = string.Empty;
        graphData = string.Empty;
        graphMetadata = new List<MetaDataNode>();
        accessRights = new List<AccessRight>();
        useCase = new UseCase();
        guidelines = new object();
    }

    /// <summary>
    /// Contains the serialized template of the graph. The file must have valid Turtle syntax.
    /// </summary>
    [Required]
    public string GraphTemplate
    {
        get => graphTemplate;
        set
        {
            if (IsValidTurtle(value))
            {
                graphTemplate = value;
            }
            else
            {
                throw new ArgumentException("The Turtle file contains invalid syntax.");
            }
        }
    }

    /// <summary>
    /// Serialized Turtle file with instances. The file must have valid Turtle syntax that matches the template graph.
    /// </summary>
    public string GraphData
    {
        get => graphData;
        set
        {
            if (IsValidTurtle(value))
            {
                graphData = value;
            }
            else
            {
                throw new ArgumentException("The Turtle file contains invalid syntax.");
            }
        }
    }

    /// <summary>
    /// List of access rights defined for the graph.
    /// </summary>
    public List<AccessRight> AccessRights
    {
        get => accessRights;
        set
        {
            accessRights = value;
        }
    }

    /// <summary>
    /// Contains the valid use case for this exchange scenario.
    /// </summary>
    public UseCase UseCase
    {
        get => useCase;
        set
        {
            useCase = value;
        }
    }

    /// <summary>
    /// List of metadata nodes. Each node contains a unique ID, the class type, and a list of key-value pairs.
    /// </summary>
    [Required]
    public List<MetaDataNode> GraphMetadata
    {
        get => graphMetadata;
        set
        {
            if (AreValidMetadataEntries(value))
            {
                graphMetadata = value;
            }
            else
            {
                throw new ArgumentException("At least one metadata entry is invalid. All keys and values must be valid strings.");
            }
        }
    }

     /// <summary>
    /// Reduced guidelines as a List object. The DynamicKafkaProducer serializes this with System.Text.Json 
    /// using ReferenceHandler.Preserve to maintain $id/$ref/$type/$values metadata in Kafka messages.
    /// </summary>
     public object Guidelines
     {
         get => guidelines;
         set => guidelines = value;
     }

    /// <summary>
    /// Serializes the current GraphDataModel and writes it to a file.
    /// </summary>
    /// <param name="filePath">The path of the file to write to.</param>
    public void SerializeToFile(string filePath)
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true // For nicely formatted JSON
        });

        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// serializes the current GraphDataModel to json
    /// </summary>
    /// <returns>content as string</returns>
    public string SerializeToJson()
    {
        var json =  JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true // For nicely formatted JSON
        });

        return json ?? string.Empty;
    }

    /// <summary>
    /// deserializes the json to a GraphDataModel
    /// </summary>
    /// <param name="json">The json content holding a valid GraphDataModel</param>
    /// <returns>GraphDataModel based on json input</returns>
    public static GraphDataModel DeserializeFromJson(string json)
    {
        var result = JsonSerializer.Deserialize<GraphDataModel>(json);
        return result ?? new GraphDataModel();
    }

    /// <summary>
    /// Deserializes a GraphDataModel from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the file to load.</param>
    /// <returns>A GraphDataModel object created from the file.</returns>
    public static GraphDataModel DeserializeFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The file was not found.", filePath);
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<GraphDataModel>(json) ?? new GraphDataModel();
    }

    /// <summary>
    /// Validates whether the given Turtle file has valid syntax.
    /// This function uses a simple regex as an example. A detailed validation would be more specific.
    /// </summary>
    /// <param name="turtleContent">The content of the Turtle file.</param>
    /// <returns>True if the Turtle syntax is valid, otherwise False.</returns>
    private static bool IsValidTurtle(string turtleContent)
    {
        if (string.IsNullOrWhiteSpace(turtleContent))
        {
            return false;
        }

        // Simple example check for Turtle syntax (expandable)
        string turtlePattern = @"@prefix\s+\w+:\s+<.*?>\s*\.";
        return Regex.IsMatch(turtleContent, turtlePattern);
    }

    /// <summary>
    /// Validates the metadata by checking if all nodes contain valid data.
    /// </summary>
    /// <param name="metadataEntries">The list of metadata nodes.</param>
    /// <returns>True if all metadata entries are valid, otherwise False.</returns>
    private static bool AreValidMetadataEntries(List<MetaDataNode> metadataEntries)
    {
        if (metadataEntries == null || metadataEntries.Count == 0)
        {
            return false;
        }

        foreach (var node in metadataEntries)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.ClassType))
            {
                return false;
            }

            foreach (var kvp in node.PropertiesValues)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    return false;
                }
            }
        }

        return true;
    }
}