# Used Technologies

This document lists the technologies used in the Instance Service.

## Backend

- **.NET 8**: The backend is built with .NET framework.
- **ASP.NET Core**: The backend uses ASP.NET Core for building the web API.
- **Entity Framework Core**: The backend uses Entity Framework Core for data access.
- **Keycloak**: The backend uses Keycloak for authentication and authorization.
- **NUKE**: The build process is automated with NUKE.
- **Docker**: The API is containerized with Docker.
- **MinIO**: The backend uses MinIO for caching the guideline stored in MinIO with its classification data.
- **Kafka**: The backend uses Kafka to publish GraphDataModel export messages and fetching GraphDataModel import messages.
- **Swagger / Swashbuckle**: Used for API documentation and interactive Swagger UI.
- **OpenTelemetry**: Used for distributed tracing and monitoring.
- **PostgreSQL**: The relational database.
- **ArcadeDB**: The graph database, accessed via the Gremlin query language.
- **Gremlin.Net**: The .NET client library for executing Gremlin graph traversals against ArcadeDB.
- **AutoMapper**: The backend uses AutoMapper for object-to-object mapping.

## Frontend

### Core Frameworks & Libraries

- **React**: The main library for building user interfaces.
- **React DOM**: DOM bindings for React.
- **TypeScript**: The frontend is written in TypeScript, a strongly-typed superset of JavaScript for safer and more maintainable code.

### State Management & Data Fetching
- **@tanstack/react-query**: For efficient data synchronization and server state management.
- **@microsoft/signalr**: Client library for adding real-time web functionality.

### Routing & Authentication

- **react-router-dom**: For client-side routing.
- **react-oidc-context**: Handles OpenID Connect (OIDC) based authentication and user session management.
- **jwt-decode**: A small library for decoding JSON Web Tokens (JWTs) on the client-side.

### UI Component Libraries & Styling

- **@mui/material, @mui/icons-material**: Modern UI component library (Material UI) for building feature-rich interfaces, with icon support.
- **@emotion/react, @emotion/styled**: CSS-in-JS libraries for styling React components, used by Material UI.
- **tailwindcss**: A utility-first CSS framework for rapidly building custom user interfaces.
- **material-react-table**: A powerful data table component built on top of Material UI.
- **@rjsf/mui, @rjsf/validator-ajv8**: Automatically generates Material UI forms from a JSON schema.
- **sonner**: An opinionated toast component for creating beautiful, non-intrusive notifications.
- **notistack**: A highly customizable library for displaying snackbars/notifications.

### Data Visualization & Utilities

- **react-graph-vis, react-vis-network-graph, react-vis-graph-wrapper**: Libraries for rendering interactive network graphs and visualizations.
- **react-csv**: Component to generate and trigger the download of data in CSV format.

### Tooling & Developer Experience

- **vite**: Build tool and development server.
- **@vitejs/plugin-react**: React integration for Vite.
- **eslint, @typescript-eslint/eslint-plugin, eslint-plugin-react-hooks, eslint-plugin-react-refresh**: Code linting tools to enforce code quality and best practices.
- **openapi-typescript-codegen**: Generates TypeScript client code from OpenAPI specifications.
- **autoprefixer, postcss**: Post-processing tools for enhanced browser CSS compatibility.
- **@originjs/vite-plugin-federation**: Enables module federation/micro-frontend setup with Vite.