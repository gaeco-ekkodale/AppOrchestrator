# Architecture & Services

This document describes the technical architecture of the App Orchestrator and the Docker services used.

## Overview

The App Orchestrator is a microservices-oriented application that is fully containerized. It consists of a frontend, a backend, and one infrastructure service (database). The orchestrator itself communicates with the Docker daemon to manage application stacks on the host.

<!-- TODO: Insert architecture diagram here (architecture.png) -->

> **Placeholder:** Architecture diagram will be added here.

### Architecture Explanation

The system architecture is divided into three main layers:

1. **Frontend (User Interface)**:
   - The **Client App** is a Single Page Application (React + Vite) running in the user's browser.
   - It authenticates users via **Keycloak** (OIDC) and communicates with the Backend API via REST/JSON.
   - The frontend also communicates directly with the **AppRegistry API** to browse available application packages (App Store).

2. **Backend Services**:
   - The **AppOrchestrator API** (ASP.NET Core) is the central control unit. It processes requests from the frontend to deploy and manage Docker stacks.
   - **Keycloak** serves as the Identity Provider, managing user identities and validating access tokens. It is a shared service across the Gaeco platform.
   - The API interacts with the **Docker Daemon** via Docker.DotNet and Docker Compose CLI subprocesses to manage containers, networks, and images.

3. **Infrastructure (Persistence)**:
   - **PostgreSQL** stores orchestrator metadata (stacks, networks, registries).

**Note:** The App Orchestrator does **not** use object storage (no MinIO). All Docker Compose workspace files are stored on the local filesystem under the configured `Orchestrator:RootPath`.

## Docker Services

The application is orchestrated via `docker-compose`. The following containers are used:

### 1. Application Layer

| Service      | Container Name           | Technology         | Description                                                                           |
| :----------- | :----------------------- | :----------------- | :------------------------------------------------------------------------------------ |
| **Frontend** | `apporchestrator-client` | React, Vite, Nginx | The web interface for users. Communicates with the orchestrator API and app registry. |
| **Backend**  | `apporchestrator-server` | ASP.NET Core 8     | The REST API. Manages Docker stacks, networks, and registries.                        |

### 2. Infrastructure Layer

| Service    | Container Name             | Technology    | Description                                                        |
| :--------- | :------------------------- | :------------ | :----------------------------------------------------------------- |
| **App DB** | `apporchestrator-postgres` | PostgreSQL 14 | Stores orchestrator metadata (stacks, networks, registry entries). |

### 3. Shared Services (External)

| Service               | Technology | Description                                                                            |
| :-------------------- | :--------- | :------------------------------------------------------------------------------------- |
| **Identity Provider** | Keycloak   | Manages users and authentication (OIDC/OAuth2). Shared with the entire Gaeco platform. |
| **Reverse Proxy**     | Traefik    | Routes external traffic to all services. Required in production.                       |

## Communication & Network

All services communicate via Docker networks:

- **`internal`**: Internal network for database access (API ↔ PostgreSQL).
- **`traefik-proxy`**: External network via which Traefik routes requests to the API.
- **`gaeco-network`**: Shared Gaeco platform network (used by the frontend client).

Communication flows:

- **Frontend → Orchestrator API:** REST API calls (via Browser/Traefik).
- **Frontend → AppRegistry API:** REST API calls to browse available packages.
- **Frontend → Keycloak:** OIDC Login Flow (Redirects).
- **Backend → Keycloak:** Token validation & user info.
- **Backend → Docker Daemon:** Docker.DotNet + Docker Compose CLI (container management).
- **Backend → PostgreSQL:** Entity Framework Core (data persistence).

## Filesystem (Volumes)

To avoid data loss when restarting containers, Docker Volumes and bind mounts are used:

| Mount                                                    | Description                                                                |
| :------------------------------------------------------- | :------------------------------------------------------------------------- |
| `./volumes/orchestrator/postgres`                        | PostgreSQL data directory.                                                 |
| `./volumes/orchestrator/server` → `/orchestrator`        | Workspace for Docker Compose project files (`.env`, `docker-compose.yml`). |
| `./volumes/orchestrator/docker-config` → `/root/.docker` | Docker credential store for authenticated image pulls.                     |
| `/var/run/docker.sock`                                   | Docker socket bind-mount enabling the API to control the Docker daemon.    |

## Technology Stack

- **Backend:** C# / .NET 8, Entity Framework Core, FastEndpoints, Docker.DotNet.
- **Frontend:** TypeScript, React, Material UI, Vite.
- **Auth:** OpenID Connect (OIDC) with Keycloak.
- **Routing:** Traefik (reverse proxy).
