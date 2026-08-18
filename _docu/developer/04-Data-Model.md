# Data Model

This document describes the data models of the Instance Service.

# Models

## Instance

The `Instance` class represents an instance node with its properties and relations.

- **Id** (`string`): The unique identifier for the instance node.
- **Name** (`string`): The name of the instance node.
- **ClassificationId** (`string`): The identifier of the classification to which this instance node belongs.
- **Properties** (`Dictionary<string, string>`): The properties of the instance node as a key-value pair dictionary.
- **Relations** (`List<InstanceRelation>`): The list of relations to other instance nodes.

## InstanceMetaData

The `InstanceMetaData` class represents the metadata of an instance.

- **Id** (`string`): The ID of the instance.
- **Name** (`string`): The name of the instance.
- **ClassificationId** (`string`): The classification ID of the instance.
- **Properties** (`Dictionary<string, string>`): The properties of the instance.

## InstanceRelation

The `InstanceRelation` class represents a directed relationship between two instances. This class overrides `Equals` and `GetHashCode` to provide value-based equality on the combination of `SubjectId`, `ObjectId`, and `Label`.

- **SubjectId** (`string`): The id of the subject node of the relation.
- **ObjectId** (`string`): The id of the object node of the relation.
- **Label** (`string`): The label of the relation.

## GraphDataModel

The `GraphDataModel` class is the core event model of `Gaeco`, used for the entire communication across Gaeco services to import and export GraphData. It encapsulates the graph's structure, its instance data, associated metadata, access control rights, and the use case it belongs to.

- **GraphTemplate** (`string`): A string representing the graph's schema in Turtle (TTL) format.
- **GraphData** (`string`): A string containing the instance data of the graph, also in Turtle (TTL) format.
- **AccessRights** (`List<AccessRight>`): A list of `AccessRight` objects that define the access control permissions for this graph data.
- **UseCase** (`InstanceService.Models.UseCase`): The specific `UseCase` object associated with this graph data, providing context.
- **GraphMetadata** (`List<MetaDataNode>`): A list of `MetaDataNode` objects that provide descriptive metadata for the graph.
- **Guidelines** (`object`): The reduced guidelines relevant for this graph data exchange. Serialized with `System.Text.Json` using `ReferenceHandler.Preserve` to maintain reference metadata in Kafka messages.

## MetaDataNode

The `MetaDataNode` class represents a single, identifiable node of metadata within the graph. It links an entity's ID and class type with a collection of key-value properties.

- **Id** (`string`): The unique identifier of the metadata node or the entity it describes.
- **ClassType** (`string`): The classification or type of the entity (e.g. the name of the class in an ontology).
- **PropertiesValues** (`Dictionary<string, string>`): A dictionary containing key-value pairs that represent the properties and their corresponding values for this metadata node.

## AccessRight

Represents the database table definition for an AccessRight.

- **Id** (`string`): The ID of the AccessRight.
- **Name** (`string`): The name of the guideline-classification-property.
- **GuidelineClassificationId** (`string`): The ID of the GuidelineClassification.
- **UserGroupId** (`Guid`): The ID of the Usergroup the AccessRight belongs to.
- **UseCaseId** (`Guid`): The ID of the Use Case the AccessRight belongs to.
- **GuidlineClassificationPropertyId** (`string`): The ID of the Guideline Classification Property.
- **Right** (`PropertyRight`): The access permission for the property.

For more information consult the `AccessRight Service` documentation.

## UseCase

The `UseCase` represents a distinct use case within the system. It has the following properties:

- **Id** (`string`): The unique identifier of the use case.
- **Name** (`string`): The name of the use case.
- **Description** (`string`): A description of the use case.

For more information consult the `UseCase Service` documentation.

# Enumerations

## Accessibility

The `Accessibility` enum specifies the accessibility level for an instance, determined by the available property rights.

- **None** (`0`): No access is granted.
- **ReadOnly** (`1`): Read-only access is granted.
- **ReadWrite** (`2`): Both read and write access are granted.
- **FullControl** (`3`): Full access is granted.

## Direction

The `Direction` enum is currently only used for Automapper purposes and specifies the direction of a relationship relative to an instance.

- **From**: The relationship from an existing instance.
- **To**: The relationship to an existing instance.