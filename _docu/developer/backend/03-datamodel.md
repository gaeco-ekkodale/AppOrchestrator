# Data Model

This document describes the structured representation of data and relationships within the AppOrchestrator system. It uses Entity Framework Core to define the data model and database schema. Code-first migrations are used to evolve the database schema over time.

## Entities

### Stack _(Abstract Base)_

The abstract base for every persisted stack. Uses EF Core **TPH (Table-Per-Hierarchy)** with a `StackType` discriminator column. All stack types share a single `Stacks` table.

- **Id**: Primary key (GUID).
- **StackName**: User-facing display name.
- **DockerProjectName**: The Docker Compose project name (derived from `StackName`, used as the unique identifier for Docker operations).
- **NetworkName**: Foreign key to the `Network` this stack belongs to.
- **CreatedAt**: UTC timestamp of creation.
- **UpdatedAt**: UTC timestamp of the last metadata update.

### RegistryStack _(inherits Stack)_

A stack deployed from a versioned package in an `AppRegistry`. Compose editing is blocked; updates are done exclusively via version bumps.

- **AppRegistryId**: Foreign key to the source `AppRegistry`.
- **PackageId**: Package identifier in the source registry (e.g., `com.example.my-app`).
- **PackageVersion**: The deployed package version (e.g., `1.0.0`).

### CustomStack _(inherits Stack)_

A stack deployed from a user-supplied `docker-compose.yml`. Direct compose editing is permitted.

_(No additional fields beyond the base `Stack`.)_

### Network

Represents a user-created Docker network managed by the orchestrator. Only networks explicitly created through the API are persisted here.

- **Name**: Primary key – the Docker network name. Immutable after creation.
- **CreatedAt**: UTC timestamp of creation.
- **UpdatedAt**: UTC timestamp of the last metadata update.
- **EnvironmentVariables**: List of shared environment variables (owned type) injected into every stack deployed on this network.
- **Stacks**: Collection of stacks assigned to this network.

### EnvironmentVariable _(Owned by Network)_

Represents an environment variable key-value pair associated with a `Network`. Configured as an EF Core owned type.

- **Name**: Variable name (e.g., `DATABASE_HOST`).
- **Value**: Variable value.

### AppRegistry

Represents an external application registry that serves deployable packages (e.g., an AppRegistry instance).

- **Id**: Primary key (GUID).
- **Name**: Display name shown in the UI.
- **BaseUrl**: Base URL of the registry API used to resolve package files.
- **CreatedAt**: UTC timestamp of creation.
- **Stacks**: Collection of `RegistryStack` entities currently linked to this registry.

### ContainerRegistry

Represents a Docker container image registry that the orchestrator can pull images from. Credentials are **not** stored – they are passed through to `docker login` at registration time and stored in the Docker credential store.

- **Id**: Primary key (GUID).
- **Name**: Display name of the registry entry.
- **ServerAddress**: The registry server address (e.g., `myregistry.azurecr.io`).
- **CreatedAt**: UTC timestamp of creation.

## Relationships

<!-- TODO: Insert data model diagram here (model-relationships.png) -->

> **Placeholder:** Data model diagram will be added here.

## Database Context

The `AppOrchestratorDbContext` manages these entities and their configurations, including:

- TPH inheritance mapping for `Stack`, `RegistryStack`, and `CustomStack`.
- Owned type configuration for `EnvironmentVariable` on `Network`.
- Foreign key constraints and cascade delete behaviors.
- Navigation properties between `Network`, `Stack`, `RegistryStack`, and `AppRegistry`.
