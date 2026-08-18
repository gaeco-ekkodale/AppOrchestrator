<div align="center">
  <img src="https://raw.githubusercontent.com/gaeco-ekkodale/.github/main/assets/gaeco_logo_horizontal_color.png" width="200" alt="gaeco logo">

  # AppOrchestrator

  <em>Deploys and manages the Docker Compose based application stacks, shared networks and container registries of a gaeco installation.</em>

  [![License](https://img.shields.io/badge/license-fair--code-blue.svg)](LICENSE.md)
  [![Version](https://img.shields.io/github/v/release/gaeco-ekkodale/AppOrchestrator)](../../releases)

  [gaeco-ekkodale Organization](https://github.com/gaeco-ekkodale) · [All Repos](https://github.com/orgs/gaeco-ekkodale/repositories)
</div>

---

gaeco (Graphs for Architecture, Engineering, Construction, Operations) is an event-driven microservice platform for BIM data management. It translates external building-industry standards (IFC, IBPDI, Brick Schema, ASHRAE 223 and others) into a shared, versioned classification and relationship model (Guideline + Ontology) and exposes consistent, graph-based building data (Instance) across use cases and departments — without forcing every consumer onto one rigid schema. Built for organizations managing building/portfolio data across disconnected departmental systems (construction, facilities management, leasing, accounting) that need automatic, reliable data propagation instead of manual, error-prone hand-offs.

> This project is licensed under the [Source Available](LICENSE.md). Source code is viewable and usable; commercial use is restricted.

---

AppOrchestrator is the deployment plane of the gaeco platform: it deploys and manages the Docker Compose stacks the platform's services run in, and it is what binds micro-frontend clients into the [PluginHost](https://github.com/gaeco-ekkodale/PluginHost) at runtime.

The project provides a React frontend and a .NET 8 backend API for working with deployed stacks, shared Docker networks, external application registries, and private container registries. Authentication is handled via Keycloak.

## Overview

AppOrchestrator focuses on the operational side of application delivery:

- Deploy stacks from compose definitions or from an external application registry
- Manage existing stacks and inspect their details
- Maintain shared Docker networks and network-level environment variables
- Configure application registries used as deployment sources
- Configure private container registries used for image pulls

This repository contains the orchestrator itself. It is not the registry service for publishing application packages. The frontend can integrate with a separate AppRegistry instance when deployments should be created from published applications.

## Tech Stack

- Backend: .NET 8, FastEndpoints, FluentValidation, Entity Framework Core
- Frontend: React 18, TypeScript, Vite, Material UI, React Query
- Infrastructure: Docker, PostgreSQL, Keycloak, Traefik

## Repository Structure

- `Client/`: React frontend
- `Service/Api/`: backend API
- `Service/Domain/`: domain models and repository contracts
- `Service/Infrastructure/`: database access and infrastructure implementations
- `Service/Api.Tests/`: backend API tests
- `Service/Infrastructure.Tests/`: infrastructure tests
- `_docker/`: local and server Docker Compose setup
- `_pipeline/`: CI/CD pipeline definitions
- `build/`: Nuke build scripts

## Local Development

### Prerequisites

- Docker Desktop
- Node.js and npm
- .NET 8 SDK

### Start with Docker Compose

1. Change into the Docker directory:

   ```bash
   cd _docker
   ```

2. Start the local environment:

   ```bash
   docker compose -f docker-compose.yml -f docker-compose-override.yml up -d
   ```

3. Open the application:

- Frontend: http://localhost:3000
- Backend Swagger: http://localhost:6241/swagger
- Keycloak: http://localhost:9345

The local Docker setup starts PostgreSQL, Keycloak, the backend API, and the frontend client.

### Run frontend locally without Docker

From `Client/`:

```bash
npm install
npm run dev
```

The development configuration expects the orchestrator API at `http://localhost:6241` and Keycloak at `http://localhost:9345/realms/gaeco`.

### Run backend locally without Docker

From `Service/Api/`:

```bash
dotnet run
```

For local execution, the backend still needs reachable dependencies such as PostgreSQL and Keycloak.

## Main Capabilities

- Stack deployment and lifecycle management
- Deployment from raw Docker Compose content
- Stack cloning and compose updates
- Docker network management
- Shared environment configuration per network
- App registry management for external package sources
- Container registry management with credential validation
- OpenAPI/Swagger documentation with OAuth integration

## Build and Test

- Frontend build: run `npm run build` in `Client/`
- Frontend lint: run `npm run lint` in `Client/`
- Backend tests: run `dotnet test` from the repository root or inside `Service/`
- Repository build automation: use `build.cmd`, `build.ps1`, or `build.sh`

## Notes for Deployment

- The production-style Docker setup expects an external Traefik network.
- The backend mounts the Docker socket and Docker config directory to manage runtime containers and registry logins.
- Authentication and authorization are configured through Keycloak.
