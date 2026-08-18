# Patterns

This document describes the design patterns used in the Instance Service.

## Repository Pattern

The repository pattern is used in the backend to abstract the data access layer. The `Server/Domain/IRepositories/IInstanceRepository` interface defines the methods for accessing Instance data, and the `Server/Data/Repositories/GremlinInstanceRepository` class provides the implementation using ArcadeDB via the Gremlin query language. This pattern allows to easily switch the database implementation without changing the business logic.

## Options Pattern

The options pattern is used to configure the application. The `KeycloakOptions`, `KafkaOptions`, `AccessOptions`, `GuidelineOptions`, `GremlinOptions`, `OntologyOptions`, `PostgresOptions` and `UsecaseOptions` classes define the configuration options, and the `appsettings.json` file provides the values. This pattern allows to change the configuration without recompiling the application.

## Mediator Pattern

The mediator pattern is implemented using [MassTransit](https://masstransit-project.com/). In our service, requests are sent via MassTransit’s mediator component (`IMediator`). Handlers for each query are implemented separately, encapsulating business logic for each operation. The mediator receives these requests and dispatches appropriate responses.