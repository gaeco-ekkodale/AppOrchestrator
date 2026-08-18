# Deployment

This document provides instructions for deploying the AppOrchestrator software.

## Containerization

The application is containerized using Docker. The `Dockerfile` is located in `Service/Api/Dockerfile`.

### Build Image

```bash
docker build -t apporchestrator-server -f Service/Api/Dockerfile ./Service
```

## Docker Compose

For local development and simplified deployment, Docker Compose files are provided in the `_docker` directory.

### Local Development

Uses `docker-compose.yml` combined with `docker-compose-override.yml`. The override file builds images from local source and exposes ports on `localhost`.

```bash
cd _docker
docker compose -f docker-compose.yml -f docker-compose-override.yml up -d --build
```

### Server / Production

Uses `docker-compose.yml` with a `.env.server` file. Images are pulled from the container registry instead of being built locally.

```bash
cd _docker
docker compose --env-file .env.server -f docker-compose.yml up -d
```

## Environment Variables

The application is configured via environment variables. Key variables include:

| Variable                                     | Description                                                | Example                        |
| :------------------------------------------- | :--------------------------------------------------------- | :----------------------------- |
| `Postgres__Host`                             | PostgreSQL hostname                                        | `apporchestrator-postgres`     |
| `Postgres__Port`                             | PostgreSQL port                                            | `5432`                         |
| `Postgres__User`                             | PostgreSQL user                                            | `apporchestratoruser`          |
| `Postgres__Password`                         | PostgreSQL password                                        | `...`                          |
| `Postgres__Database`                         | PostgreSQL database name                                   | `apporchestratordb`            |
| `Keycloak__Host`                             | Keycloak base URL                                          | `https://keycloak.example.com` |
| `Keycloak__Realm`                            | Keycloak realm name                                        | `gaeco`                        |
| `Keycloak__ClientId`                         | Keycloak client ID                                         | `app-orchestrator`             |
| `Orchestrator__RootPath`                     | Filesystem path for stack workspace files                  | `/orchestrator`                |
| `Orchestrator__TraefikNetwork`               | Docker network name for Traefik routing                    | `traefik-proxy`                |
| `Orchestrator__DockerHostUri`                | Docker daemon endpoint                                     | `unix:///var/run/docker.sock`  |
| `Orchestrator__VersionUpdateBackupRetention` | Number of compose file backups to retain on version update | `5`                            |

## Docker Socket

The container **must** have access to the Docker socket to manage stacks on the host. This is configured via a bind mount:

```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock
```

> **Security Note:** Mounting the Docker socket grants the container full control over the Docker daemon on the host. Ensure the orchestrator is only accessible to trusted administrators.

## CI/CD Pipelines

Pipelines are defined in the `_pipeline` directory:

- `CI_Server.yml`: Continuous Integration for the server (Build & Test).
- `CD_Server.yml`: Continuous Deployment for the server (Publish & Deploy).
- `CI_Client.yml`: Continuous Integration for the frontend (Build & Lint).
- `CD_Client.yml`: Continuous Deployment for the frontend (Publish & Deploy).
