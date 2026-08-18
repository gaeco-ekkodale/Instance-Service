<div align="center">
  <img src="https://raw.githubusercontent.com/gaeco-ekkodale/.github/main/assets/gaeco_logo_horizontal_color.png" width="200" alt="gaeco logo">

  # InstanceService

  <em>Manages the building data graph - nodes, properties and relationships - validated against Guideline and Ontology.</em>

  [![License](https://img.shields.io/badge/license-fair--code-blue.svg)](LICENSE.md)
  [![Version](https://img.shields.io/github/v/release/gaeco-ekkodale/Instance-Service)](../../releases)

  [gaeco-ekkodale Organization](https://github.com/gaeco-ekkodale) · [All Repos](https://github.com/orgs/gaeco-ekkodale/repositories)
</div>

---

gaeco (Graphs for Architecture, Engineering, Construction, Operations) is an event-driven microservice platform for BIM data management. It translates external building-industry standards (IFC, IBPDI, Brick Schema, ASHRAE 223 and others) into a shared, versioned classification and relationship model (Guideline + Ontology) and exposes consistent, graph-based building data (Instance) across use cases and departments — without forcing every consumer onto one rigid schema. Built for organizations managing building/portfolio data across disconnected departmental systems (construction, facilities management, leasing, accounting) that need automatic, reliable data propagation instead of manual, error-prone hand-offs.

> This project is licensed under the [Source Available](LICENSE.md). Source code is viewable and usable; commercial use is restricted.

---

## What this service does

The InstanceService holds the actual building data. Where the [GuidelineService](https://github.com/gaeco-ekkodale/GuidelineService) and [OntologyService](https://github.com/gaeco-ekkodale/OntologyService) define the model, this service manages the instances of that model — nodes with their properties and their relationships — per use case.

Storage is deliberately split to handle large graphs:

- **ArcadeDB** (accessed via the Gremlin query language) stores nodes and relational data
- **PostgreSQL** stores the properties

Every write is validated before it lands: class types are checked against the guideline's classifications, every subject–predicate–object triple is checked against the ontology's allowed relationships, and the caller's create, property and relationship permissions are checked against the [AccessService](https://github.com/gaeco-ekkodale/AccessService). See [GraphDataModelValidationService](_docu/developer/GraphDataModelValidationService.md) for the details.

Nodes, properties and relationships can be edited in the Instance client, and the service participates in the platform's event flow in both directions: it publishes graph data updates to a Kafka export topic and consumes updates from import topics.

New to the project? Start with [`_docu/developer/01-Concepts.md`](_docu/developer/01-Concepts.md).

## Repository Structure

- `Server/Api/`: ASP.NET Core Web API and SignalR hub
- `Server/Domain/`: domain logic, including graph data model validation
- `Server/Data/`: ArcadeDB (Gremlin) and PostgreSQL data access
- `Server/Models/`: shared models
- `Server/Events/`: Kafka event contracts
- `Server/Api.Tests/`, `Server/Data.Tests/`: unit tests
- `Client/`: React micro-frontend, exposed via Module Federation
- `_docker/`: Compose definition, env schemas and the App Registry package manifest
- `_docu/`: developer and user documentation
- `_pipeline/`: Azure DevOps CI/CD pipeline definitions
- `build/`: NUKE build scripts

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core, Gremlin.Net, AutoMapper, Swagger/Swashbuckle, OpenTelemetry
- **Frontend**: React, TypeScript, Vite, Material UI, material-react-table, Tailwind CSS, React Query, SignalR, react-graph-vis, Module Federation
- **Infrastructure**: ArcadeDB, PostgreSQL, MinIO, Apache Kafka, Keycloak, Docker
- **Build**: NUKE

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- Node.js 20+
- The shared platform infrastructure (Keycloak, MinIO, Kafka) plus GuidelineService, OntologyService, AccessService, UseCaseService, PluginHost and AppOrchestrator — see [`_docu/user/01-Installation.md`](_docu/user/01-Installation.md)

### Start with Docker Compose

```bash
cd _docker
docker compose -p instance-service -f docker-compose.yml -f docker-compose-override.yml up -d
```

This brings up PostgreSQL, ArcadeDB (HTTP, binary and Gremlin ports), the API and the client. Ports are driven by the `INSTANCE_*_OUTERPORT` variables in the environment files; the API exposes Swagger at `/swagger`.

### Run the client locally

```bash
cd Client
npm ci
npm run dev
```

The client is a micro-frontend and no longer needs to be uploaded manually through the PluginManager. In an integrated setup the `instance-client` container publishes its micro-frontend metadata, which the AppOrchestrator discovers and binds into the PluginHost automatically.

## Build and Test

```bash
./build.sh     # Linux/macOS
.\build.ps1    # Windows
```

- Backend tests: `dotnet test` from the repository root
- Frontend build: `npm run build` in `Client/`

## Integration

- **Authentication**: Keycloak (OIDC/JWT). The PluginHost authenticates the user and performs a token exchange to obtain a token scoped to the `instance-client` plugin. Authentication is active whenever `ASPNETCORE_ENVIRONMENT` is not `Development`.
- **Events (export)**: when a CRUD operation has been applied and the trigger-data-updated endpoint is called, all GraphDataModels are published to a Kafka export topic.
- **Events (import)**: the service listens on import topics and incorporates external updates where possible.
- **Model sources**: classifications come from the guideline cached from MinIO; relationship rules come from the ontology.

## Documentation

- [Concepts](_docu/developer/01-Concepts.md)
- [Patterns](_docu/developer/02-Patterns.md)
- [Used Technologies](_docu/developer/03-Used-Technologies.md)
- [Data Model](_docu/developer/04-Data-Model.md)
- [Software Architecture](_docu/developer/05-Software-Architecture.md)
- [GraphDataModel Validation](_docu/developer/GraphDataModelValidationService.md) · [Completeness Check](_docu/developer/GraphDataModel-Completeness-Check.md)
- [Installation](_docu/user/01-Installation.md) · [User Manual](_docu/user/02-User-Manual.md) · [Federated Query Engine](_docu/user/03-Federated-Query-Engine.md)
