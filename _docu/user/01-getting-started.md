# Getting Started (Local Setup)

This guide describes how to start the App Orchestrator infrastructure locally on your machine.

## Prerequisites

- **Docker Desktop** (or Docker Engine + Docker Compose Plugin) installed and running.
- **Git** (optional, for cloning the repository).
- A running **Keycloak** instance (shared Gaeco platform service, see the platform setup guide).

## Installation & Start

The entire infrastructure is defined in the `_docker` folder.

1. Open a terminal (PowerShell or Bash).
2. Navigate to the `_docker` directory:
   ```bash
   cd _docker
   ```
3. Start the services with Docker Compose:
   ```bash
   docker compose -f docker-compose.yml -f docker-compose-override.yml up -d
   ```

> **Note:** The `docker-compose-override.yml` file is specifically configured for local development. It exposes ports on `localhost`, builds images from local source, and configures the Docker socket path for Windows/Linux.

## Accessing the Services

After the containers have started, you can access the services at the following addresses:

| Service                 | URL                                                            | Description                         | Credentials (Default)                   |
| :---------------------- | :------------------------------------------------------------- | :---------------------------------- | :-------------------------------------- |
| **App Orchestrator UI** | [http://localhost:3000](http://localhost:3000)                 | The frontend for users.             | Login via Keycloak                      |
| **Backend API**         | [http://localhost:6241/swagger](http://localhost:6241/swagger) | Swagger UI of the API.              | Bearer token from Keycloak required     |
| **Keycloak**            | [http://localhost:9345](http://localhost:9345)                 | Identity Provider (shared service). | User: `keycloakadmin` / `keycloakadmin` |

## Stopping the Services

To stop the environment:

```bash
docker compose -f docker-compose.yml -f docker-compose-override.yml down
```
