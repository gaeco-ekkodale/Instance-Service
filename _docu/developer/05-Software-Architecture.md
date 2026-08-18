# Software Architecture

This document describes the software architecture of the Instance Service.

## Overview

The Instance Service consists of a frontend client and a backend service that provides a REST API for managing graph data. The backend is implemented with .NET and is organized into several distinct projects representing different layers of a clean architecture.

## Backend Architecture

The backend is a modular, multi-project solution and consists of the following layers:

- **API Layer (`InstanceService.Api`)**:  
  This layer is responsible for handling incoming HTTP requests and sending responses. It contains controllers to process API calls, middleware, validators, and utilizes Data Transfer Objects (DTOs) for communication. The API layer is the main entry point for client interactions.
- **Data Layer (`InstanceService.Data`)**:  
  This layer handles data persistence. It contains the Entity Framework `InstanceServiceDbContext`, database `Migrations`, and concrete `Repositories` that implement the interfaces defined in the domain layer.
- **Domain Layer (`InstanceService.Domain` & `InstanceService.Models`)**:  
  This layer is split into two projects. `InstanceService.Models` contains the core domain entities (e.g., `Instance`, `InstanceMetadata`) and enumerations. `InstanceService.Domain` defines the contracts for data access through repository interfaces (`IRepositories`).
- **Test Projects (`InstanceService.Api.Tests`, `InstanceService.Data.Tests`)**:  
  These separate projects contain unit and integration tests for their respective layers to ensure code quality and the correctness of the implementation.

## Frontend Architecture

The frontend is a single-page application (SPA) that is built with React. It uses the following components:

- **App**: The root component of the application.
- **StandaloneApp**: The root component of the application for local development without pluginhost.
- **Features**: Components that define client logic.
- **Components**: The reusable components of the application.
- **Services / API Clients**: The API clients that communicate with the backend.
- **Models**: Data models intended for use across the client.
- **Assets**: Large files or images referenced inside the client.

## Communication

The frontend communicates with the backend via a REST API.