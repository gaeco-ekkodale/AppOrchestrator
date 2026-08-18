# Software Architecture

This document describes the high-level architecture and communication flow of the AppOrchestrator Service.

## Architectural Pattern

The AppOrchestrator backend follows the **Vertical Slice Architecture** pattern, which organizes the codebase by features (slices) rather than technical layers. Each feature contains all necessary components (Endpoints, DTOs, Validators, Mappers) to handle a specific use case.
This approach improves maintainability and scalability by keeping related code together.

The project is structured into three main layers:

## Layer Responsibilities

### 1. API Layer (`Service/Api`)

- **Entry Point**: Handles HTTP requests.
- **Endpoints**: Contains the vertical slices organized by domain (`Stacks`, `Networks`, `AppRegistries`, `ContainerRegistries`).
- **Services**: Application services for Docker operations (`Docker/`, `Stacks/`, `Mfe/`, `Registry/`, `Storage/`).
- **Shared**: Cross-cutting DTOs, mappers, and routing helpers.
- **Configuration**: Dependency Injection setup, middleware configuration (`Core/Extensions/`).

### 2. Domain Layer (`Service/Domain`)

- **Core Entities**: Contains pure business entities (`Stack`, `RegistryStack`, `CustomStack`, `Network`, `AppRegistry`, `ContainerRegistry`, `EnvironmentVariable`).
- **Repository Interfaces**: Defines contracts for data access (located in `Domain/Repositories/`).
- **No Dependencies**: Does not depend on database or API concerns.

### 3. Infrastructure Layer (`Service/Infrastructure`)

- **Implementation**: Implements repository interfaces defined in the Domain layer.
- **Database**: `AppOrchestratorDbContext` and EF Core migrations.
- **No External Integrations**: Unlike the AppRegistry, there is no object storage. Files are written directly to the filesystem.

## Communication Flow

1. **Request**: A request hits an Endpoint in the API layer.
2. **Validation**: FluentValidation validates the incoming request.
3. **Processing**: The Endpoint calls an Application Service (e.g., `IStackDeploymentService`).
4. **Docker Operations**: The service uses Docker.DotNet and Docker Compose CLI to interact with the Docker daemon.
5. **Data Access**: The Infrastructure layer reads/writes stack metadata via EF Core.
6. **Response**: The result is mapped to a Response DTO and returned to the client.

## Key Services

| Service Interface         | Responsibility                                                          |
| :------------------------ | :---------------------------------------------------------------------- |
| `IStackDeploymentService` | Orchestrates full stack lifecycle: deploy, update, stop, start, delete. |
| `IDockerProjectService`   | Queries live Docker project/container status via Docker.DotNet.         |
| `IMfeSyncService`         | Synchronizes Micro-Frontend metadata from running container labels.     |
| `IRegistryService`        | Fetches compose files and env schemas from an AppRegistry.              |
| `IStorageService`         | Reads/writes workspace files (compose, .env) on the filesystem.         |
