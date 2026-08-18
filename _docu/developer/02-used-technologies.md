# Used Technologies

This document lists the key technologies, frameworks, and libraries used in the AppOrchestrator Service.

## Core Framework

- **.NET 8**: The underlying runtime and framework.
- **ASP.NET Core Web API**: Used for building the RESTful API.

## Database

- **PostgreSQL**: The primary relational database management system.
- **Entity Framework Core (Npgsql)**: The Object-Relational Mapper (ORM) used for data access and migrations.

## API & Architecture

- **FastEndpoints**: A developer-friendly alternative to Minimal APIs and MVC Controllers, enforcing the REPR pattern.
- **FastEndpoints.Swagger**: Generates OpenAPI documentation (Swagger UI) automatically.
- **AutoMapper**: Used for object-to-object mapping (e.g., mapping Entities to DTOs).
- **FluentValidation**: A popular .NET library for building strongly-typed validation rules.

## Docker Integration

- **Docker.DotNet**: .NET client library for the Docker Engine API. Used to manage containers, networks, and images programmatically.
- **Docker CLI (subprocess)**: Docker Compose operations (up, down, pull) are executed via `docker compose` CLI subprocesses, since Docker Compose is not natively available through Docker.DotNet.

## Authentication & Security

- **JWT Bearer Authentication**: Secures the API endpoints using JSON Web Tokens.
- **Keycloak**: External Identity Provider for OIDC-based authentication.

## Frontend

- **React**: A JavaScript library for building user interfaces.
- **TypeScript**: Adds static typing to JavaScript for better developer experience and code quality.
- **Vite**: Next Generation Frontend Tooling, used for fast development and building.
- **Material UI (MUI)**: A comprehensive library of React UI components that implements Google's Material Design.
- **React Query (@tanstack/react-query)**: Powerful asynchronous state management for fetching, caching, and updating server state.
- **React Router**: Standard routing library for React applications.
- **React OIDC Context**: Simplifies OpenID Connect (OIDC) authentication in React apps.
- **Vite Plugin Federation**: Enables Module Federation support for Micro-Frontend architecture.
- **OpenAPI TypeScript Codegen**: Generates typed API clients from the backend Swagger definition.

## DevOps & Tooling

- **Docker**: Used for containerizing the application and its dependencies.
- **Docker Compose**: Orchestrates the multi-container environment (API, Database).
- **Traefik**: Reverse proxy used in production for routing and TLS termination.
