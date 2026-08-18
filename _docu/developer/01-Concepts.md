# Concepts

This document describes the main concepts used in the Instance Service.

## Managing graph data

The `Instance Service` is designed to manage graph data for a specific use case in detail. It connects ArcadeDB (via the Gremlin query language) with a PostgreSQL database to enable the storage of large quantities of graph data. Properties and relational data are stored separately: relational data is stored exclusively in ArcadeDB, while properties are stored solely in PostgreSQL. Nodes, along with their properties and relationships, can be edited in the `Instance Client`. A Kafka message will be published whenever the graph data of a use case is updated. Other systems can subscribe to the export topic to receive graph data updates. The `Instance Service` will also listen to import topics and incorporate updates when possible. The instance service works based on both a guideline and an ontology.

## Micro-Frontends

The Client of the Service is designed as a micro-frontend. It no longer needs to be uploaded manually through the `PluginManager`. For development, it can be started locally via the Vite development server. For integrated runtime scenarios, the `instance-client` container exposes microfrontend metadata that is discovered by the `AppOrchestrator`, which then binds the client into the `PluginHost` automatically.

## Authentication and Authorization

### Inside Backend

Authentication and authorization are handled by Keycloak. Before requesting data from the `Instance Service`, a client must authenticate. Authentication can be enabled by setting the `ASPNETCORE_ENVIRONMENT` to any value other than `Development`.

### Inside Client

The `PluginHost` authenticates the user and then requests an access token specifically for the `instance-client` Plugin by making a token exchange with the user token. The plugins can then use this token to authorize the user within the `Instance Api`.

## Event Driven Design with Kafka

The `Instance Service` uses an event-driven architecture to communicate changes in graph data across the system. This is implemented using [Apache Kafka](https://kafka.apache.org/) as the message broker.

### Kafka Events

Whenever a CRUD operation is applied and the trigger-data-updated endpoint is triggered, all GraphDataModels are published to a Kafka export topic. This event allows other services to subscribe to GraphDataModel changes, promoting loose coupling and enabling real-time reactions elsewhere in the platform.