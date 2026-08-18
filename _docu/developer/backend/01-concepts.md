# Concepts

This document outlines the core architectural concepts and patterns used in the AppOrchestrator Service.

## Vertical Slice Architecture

The application follows a **Vertical Slice Architecture**. Instead of organizing code by technical layers (Controllers, Services, Repositories), the code is organized by **Features**.

- **Endpoints Folder**: Located in `Service/Api/Endpoints`.
- **Structure**: Each feature (e.g., `Stacks`, `Networks`, `AppRegistries`) contains everything needed to implement that specific functionality, including:
  - Endpoints
  - DTOs (Data Transfer Objects)
  - Validators
  - Mappers

This approach minimizes coupling between unrelated features and makes the codebase easier to navigate and maintain.

## REPR Pattern (Request-Endpoint-Response)

We utilize the **REPR (Request-Endpoint-Response)** pattern, facilitated by the **FastEndpoints** library.

- **Endpoints**: Instead of traditional MVC Controllers with multiple actions, each API endpoint is a distinct class.
- **Request/Response**: Each endpoint defines its specific Request and Response DTOs.
- **Benefits**: This leads to thinner controllers (endpoints), better separation of concerns, and easier testing.

## Domain-Driven Design (DDD) Elements

While not a strict DDD implementation, the project uses several DDD concepts:

- **Domain Layer**: Contains the core business entities (`Stack`, `Network`, `AppRegistry`, etc.) and repository interfaces. This layer has no dependencies on external frameworks.
- **Infrastructure Layer**: Handles external concerns like database access (EF Core) and repository implementations.
- **Rich Domain Models**: Entities encapsulate their own data and basic validation logic where appropriate.

## Table-Per-Hierarchy (TPH) for Stacks

The `Stack` entity uses EF Core **Table-Per-Hierarchy (TPH)** inheritance. Both `RegistryStack` and `CustomStack` are stored in a single `Stacks` table with a `StackType` discriminator column.

- **`RegistryStack`**: Deployed from a versioned package in an `AppRegistry`. Version updates are managed by fetching a new compose file from the registry.
- **`CustomStack`**: Deployed from a user-supplied `docker-compose.yml`. Compose editing is allowed directly by the user.

## Docker Integration

The orchestrator interacts with the Docker daemon in two ways:

1. **Docker.DotNet**: Used for real-time status queries (container state, network information).
2. **Docker Compose CLI (subprocess)**: Docker Compose operations (`up`, `down`, `pull`) are executed as CLI subprocesses. This is necessary because Docker Compose is not natively available through the Docker Engine API.

Workspaces (compose files and env files) are stored on the filesystem under `Orchestrator:RootPath`. Each stack gets its own subdirectory.

## Validation

Validation is handled using **FluentValidation**. Validators are strongly typed and attached to the Request DTOs. Validation logic is executed automatically before the endpoint logic runs.

## Authorization

All endpoints require an authenticated admin user by default. Authentication is handled via **JWT Bearer** tokens issued by Keycloak. The `AdminOnly` policy is applied globally.
