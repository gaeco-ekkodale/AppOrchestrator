# Setup Guide

This guide provides instructions for setting up the development environment for the AppOrchestrator project.

## Prerequisites

Ensure you have the following installed:

- **.NET 8 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop**: Required for running dependencies (PostgreSQL) **and** for the orchestrator to manage Docker containers on the host.
- **IDE**:
  - Visual Studio 2022 (Recommended for full .NET experience)
  - JetBrains Rider
  - OR Visual Studio Code with C# Dev Kit extension.

## Getting Started

1. **Clone the Repository**

   ```bash
   git clone <repository-url>
   cd AppOrchestrator
   ```

2. **Start Infrastructure Services**
   The project depends on PostgreSQL and an external Keycloak instance. Use Docker Compose to start them.

   ```bash
   cd _docker
   docker compose -f docker-compose.yml -f docker-compose-override.yml up -d apporchestrator-postgres
   ```

   _Note: Keycloak is provided by a shared infrastructure service (see the Gaeco platform setup). Make sure Keycloak is running at `http://localhost:9345`._

3. **Apply Database Migrations**
   Navigate to the API directory and update the database.

   ```bash
   cd Service/Api
   dotnet ef database update
   ```

   _Ensure your `appsettings.Development.json` points to the local Docker PostgreSQL instance._

4. **Docker Socket Access**
   The orchestrator communicates directly with the Docker daemon. On **Windows (Docker Desktop)**, the default named pipe is used:

   ```json
   "DockerHostUri": "npipe://./pipe/docker_engine"
   ```

   On **Linux / inside a container**, use the Unix socket:

   ```json
   "DockerHostUri": "unix:///var/run/docker.sock"
   ```

   Verify the setting in `appsettings.Development.json`.

5. **Run the Application**
   - **Visual Studio / Rider**: Open `AppOrchestrator.sln`, set `AppOrchestrator.Api` as the startup project, and press F5.
   - **CLI**:
     ```bash
     cd Service/Api
     dotnet run
     ```

6. **Verify**
   Open your browser and navigate to the Swagger UI (typically `http://localhost:6241/swagger` or the port shown in the console output).

## Project Structure

- `Service/Api`: The Web API project (endpoints, configuration, DI).
- `Service/Domain`: Domain entities and repository interfaces.
- `Service/Infrastructure`: Database context, EF Core migrations, and repository implementations.
- `Client`: React frontend (TypeScript, Vite).
- `_docker`: Docker Compose files and environment configuration for local and server deployments.
