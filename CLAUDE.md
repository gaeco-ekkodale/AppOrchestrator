# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

AppOrchestrator is a full-stack application for deploying and managing Docker Compose-based application stacks. It consists of:

- **Frontend**: React 18 + TypeScript + Vite + Material UI
- **Backend**: .NET 8 + FastEndpoints + Entity Framework Core + PostgreSQL
- **Authentication**: Keycloak (OAuth2/OIDC)
- **Infrastructure**: Docker, Docker Compose, Traefik

## Project Structure

``
AppOrchestrator/
├── Client/                          # React frontend (Vite)
│   ├── src/
│   │   ├── api/orchestrator/        # Auto-generated API client from OpenAPI
│   │   ├── features/                # Feature modules
│   │   │   ├── stacks/              # Stack deployment and management
│   │   │   ├── appRegistries/       # App registry configuration
│   │   │   ├── dockerRegistries/    # Container registry configuration
│   │   │   ├── networks/            # Network management
│   │   │   ├── projectDeploy/       # Multi-app deployment orchestration
│   │   │   ├── registryClient/      # Integration with external registries
│   │   │   ├── store/               # App store functionality
│   │   │   └── shared/              # Shared components, contexts
│   │   ├── pages/                   # Page components (route-level)
│   │   ├── layout/                  # Navigation and layout
│   │   ├── theme/                   # MUI theme configuration
│   │   └── utils/                   # Routing, API helpers, error messages
│   ├── package.json                 # Dependencies, build scripts
│   ├── vite.config.ts               # Vite build config (with Module Federation)
│   └── tsconfig.json                # TypeScript configuration
│
├── Service/                         # .NET backend
│   ├── Api/                         # Main API project
│   │   ├── Endpoints/               # FastEndpoints handlers (organized by feature)
│   │   │   ├── Stacks/              # Stack endpoints (create, list, delete, etc.)
│   │   │   ├── AppRegistries/       # Registry management
│   │   │   ├── ContainerRegistries/ # Container registry endpoints
│   │   │   └── Networks/            # Network endpoints
│   │   ├── Services/                # Business logic
│   │   │   ├── Docker/              # Docker operations (compose, networking)
│   │   │   ├── Stacks/              # Stack deployment logic
│   │   │   ├── Mfe/                 # Multi-Frontend-Extension sync
│   │   │   └── RegistryApi/         # External registry integration
│   │   ├── Core/                    # Infrastructure
│   │   │   ├── Extensions/          # DI setup, auth, data access
│   │   │   ├── Middleware/          # Global exception handling
│   │   │   └── Options/             # Configuration classes
│   │   ├── Shared/                  # DTOs, Mappers, Validators
│   │   └── Program.cs               # Application entry point
│   │
│   ├── Domain/                      # Domain models (no dependencies)
│   │   ├── Models/                  # Stack, Network, Registry entities
│   │   └── Repositories/            # Repository interfaces
│   │
│   ├── Infrastructure/              # Data access (EF Core)
│   │   ├── AppOrchestratorDbContext.cs
│   │   ├── Migrations/              # Database migrations
│   │   └── Repositories/            # Repository implementations
│   │
│   ├── Api.Tests/                   # Unit tests (xUnit, NSubstitute)
│   └── Infrastructure.Tests/        # Infrastructure tests
│
├── build/                           # Nuke build automation
│   ├── Build.cs                     # Main build targets
│   ├── Build.Client.*.cs            # Frontend-specific targets
│   └── Build.Server.*.cs            # Backend-specific targets
│
├── _docker/                         # Docker Compose configurations
│   ├── docker-compose.yml           # Main composition (server-ready)
│   ├── docker-compose-override.yml  # Local development overrides
│   ├── docker-compose.server.yml    # Production-style setup
│   └── .env                         # Environment variables
│
├── _pipeline/                       # CI/CD pipeline definitions
├── AppOrchestrator.sln              # Visual Studio solution
├── build.ps1 / build.sh             # Nuke build script entry points
└── package-lock.json                # Root-level (minimal config)
``

## Key Architectural Patterns

### Backend Architecture (FastEndpoints Pattern)

1. **Endpoint-Centric Design**: Each API operation is a FastEndpoint class that inherits from `Endpoint<TRequest, TResponse, TMapper>`. This enables:
   - Request/response validation (FluentValidation)
   - Auto-mapping via FastEndpoints mappers
   - Consistent HTTP semantics

2. **Layered Services**:
   - **Endpoints** receive HTTP requests, validate, delegate to Services
   - **Services** contain business logic (Docker operations, file management, registry integration)
   - **Repositories** abstract database access (Entity Framework Core)
   - **Domain Models** are persistence-agnostic

3. **Authentication & Authorization**:
   - JWT Bearer tokens from Keycloak
   - All endpoints require `[Authorize("AdminOnly")]` by default (configured in Program.cs)
   - Tokens injected via HttpContextAccessor

4. **Database**:
   - PostgreSQL with Entity Framework Core 8
   - Table-Per-Hierarchy (TPH) inheritance for `Stack` (base) → `RegistryStack` and `CustomStack`
   - Owned types for `EnvironmentVariable` and `AllowedVersionSuffix` (stored per Network)
   - Migrations in `Service/Infrastructure/Migrations/`

5. **Docker Integration**:
   - Uses Docker.DotNet SDK to manage containers/networks
   - Mounts Docker socket (`/var/run/docker.sock` or `npipe://./pipe/docker_engine` on Windows)
   - `DockerComposeCommandRunner` executes shell commands; `DockerContainerService` uses Docker API
   - File-based workspace management for compose definitions

6. **External Registry Integration**:
   - `AppRegistryApiClient` proxies requests to external registry services
   - Registry responses are validated and secrets are protected

### Frontend Architecture (React + React Query)

1. **State Management**:
   - React Query (`@tanstack/react-query`) for server state caching and synchronization
   - Hooks for data fetching (not Redux or Zustand)
   - Local component state for UI-only concerns

2. **API Client**:
   - Auto-generated from OpenAPI/Swagger via `openapi-typescript-codegen`
   - Located in `src/api/orchestrator/` (regenerate with `npm run fetch-api`)
   - Token management: OIDC context provides access tokens to API client

3. **Feature Structure**:
   - Each feature (stacks, registries, etc.) has: `components/`, `hooks/`, `queries.ts`
   - `queries.ts` exports React Query hooks for API calls
   - Components are composed from shared MUI components
   - Hooks combine data fetching + form mutations

4. **Authentication Flow**:
   - `react-oidc-context` manages Keycloak login/logout
   - Tokens set in `OrchestratorOpenAPI.TOKEN` on every render
   - Protected routes: unauthenticated users redirect to Keycloak login

5. **Routing**:
   - React Router v7
   - Routes defined directly in `App.tsx` (`/stacks`, `/registries`, `/environments`, `/store`, etc.)
   - `StandaloneApp.tsx` mounts `App` under `VITE_MOUNT_PATH` for dev/standalone mode

6. **Theming**:
   - Material UI theme in `src/theme/appTheme.ts`
   - Global styles via CSS and Tailwind (configured in `postcss.config.js`)

7. **Error & Toast Handling**:
   - `ToastContext` provides toast notifications (imported from MUI)
   - `errorMessages.ts` maps API error codes to user-friendly German messages

### Deployment Orchestration

1. **Stack Types**:
   - **RegistryStack**: Deployed from external registry, references registry package
   - **CustomStack**: Raw docker-compose.yml uploaded by user

2. **Stack Creation Flow** (in `CreateStack` endpoint):
   - Validate version constraints against network's `AllowedVersionSuffixes`
   - Fetch compose content from registry or accept raw compose
   - Build environment from network-level vars + stack-specific overrides
   - Write to workspace directory (`/orchestrator/{projectName}/`)
   - Execute `docker compose up -d`
   - Persist `Stack` entity to database
   - Sync multi-frontend-extension host if present

3. **Multi-Frontend Extension (MFE) Sync**:
   - After deployment, sync the MFE host container in the network
   - Host is identified by label `orchestrator.host=true`
   - Synchronous and strict for MFE-containing stacks (rollback on failure)
   - Best-effort for non-MFE stacks (never blocks)

## Build & Development

### Frontend (Client/)

**Install dependencies:**
```bash
cd Client
npm install
```
**Development server:**
```bash
npm run dev
# Runs on http://localhost:3000
```
**Build for production:**
```bash
npm run build       # TypeScript check + Vite build
npm run build:dev   # Debug build
```
**Lint:**
```bash
npm run lint
```
**Fetch latest API client from Swagger:**
```bash
npm run fetch-api   # Requires backend running at http://localhost:6241
```
### Backend (Service/)

**Run backend locally:**
```bash
cd Service/Api
dotnet run
# Swagger: http://localhost:6241/swagger
# Health: http://localhost:6241/health
```
**Run tests:**
```bash
dotnet test                                    # Run all tests
dotnet test --filter TestClass=ClassName      # Run single test class
dotnet test --filter TestClass=ClassName.MethodName  # Run single test
```
**Regenerate App Registry client from Swagger:**
```bash
# From repository root:
./build.ps1 GenerateRegistryClient   # Windows
# Or from Service/Api:
dotnet nswag run nswag.json
```
### Full Project Build (Nuke)

**Build entire solution:**
```bash
./build.ps1           # Windows
./build.sh            # Linux/Mac
```
**Targets:**
- `Clean` – removes build outputs
- `ServerRestore` – restores .NET packages
- `ServerCompile` – compiles backend
- `ServerTest` – runs tests with coverage
- `ClientRestore` – npm ci
- `ClientCompile` – builds frontend
- `ServerCI` / `ClientCI` – CI pipeline targets
- `ServerCD` / `ClientCD` – CD pipeline targets

**Run specific target:**
```bash
./build.ps1 ServerCompile
```
### Docker & Local Development

**Start full local environment:**
```bash
cd _docker
docker compose -f docker-compose.yml -f docker-compose-override.yml up -d
```
**Access local services:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:6241
- Swagger: http://localhost:6241/swagger
- Keycloak: http://localhost:9345

**Stop environment:**
```bash
cd _docker
docker compose down
```
### Environment Configuration

**Frontend (.env.development, .env.production):**
- `VITE_API_URL` – Backend API base URL
- `VITE_MOUNT_PATH` – Sub-path when deployed behind reverse proxy
- `VITE_KEYCLOAK_AUTHORITY` – Keycloak realm URL
- `VITE_KEYCLOAK_CLIENT_ID` – OIDC client ID

**Backend (appsettings.Development.json, appsettings.json):**
- `Keycloak.Host`, `Keycloak.Realm`, `Keycloak.ClientId`
- `Postgres.*` – PostgreSQL connection
- `Orchestrator.RootPath` – Workspace root (local dev: `../../_docker/volumes/orchestrator/server`)
- `Orchestrator.DockerHostUri` – Docker daemon URI (Windows: `npipe://./pipe/docker_engine`, Linux: `unix:///var/run/docker.sock`)

## Common Development Tasks

### Add a New Endpoint

1. Create a class in `Service/Api/Endpoints/{Feature}/{ActionName}.cs`
2. Define `Request` and optionally `Validator : Validator<Request>`
3. Inherit from `Endpoint<TRequest, TResponse, TMapper>` (or base classes)
4. Implement `Configure()` (HTTP verb, route, security) and `HandleAsync()`
5. Create a mapper in `Service/Api/Shared/Mappers/` if needed
6. Regenerate API client: `npm run fetch-api` in Client/

### Add a New Data Entity

1. Create model class in `Service/Domain/Models/`
2. Add `DbSet<T>` to `AppOrchestratorDbContext`
3. Configure relationships in `OnModelCreating()`
4. Create migration: `dotnet ef migrations add MigrationName` (from Service/Infrastructure)
5. Create repository interface in `Service/Domain/Repositories/`
6. Implement repository in `Service/Infrastructure/Repositories/`
7. Register in DI container (Core/Extensions/RepositoryExtensions.cs)

### Add a New Frontend Feature

1. Create `src/features/{feature}/` directory with `components/`, `hooks/`, `queries.ts`
2. Export query hooks from `queries.ts`
3. Create page component in `src/pages/`
4. Add route in `App.tsx`
5. Add navigation link in `AppNavigation.tsx`

### Handle API Errors

- Backend throws exceptions in endpoints; `ExceptionHandlingMiddleware` catches and formats as JSON
- Frontend: catch `ApiError` from generated API client, use `errorMessages.ts` to translate codes
- Toast notifications via `ToastContext.showToast()` for user feedback

## Testing

### Backend Tests

- xUnit framework, NSubstitute for mocking
- Test projects: `Service/Api.Tests/`, `Service/Infrastructure.Tests/`
- Organize tests by endpoint or service layer
- Use test helpers for setup (e.g., `AppRegistryEndpointTestHelper.cs`)

### Frontend Tests

- No test framework currently configured (consider Jest + React Testing Library if needed)
- Manual testing via dev server or Storybook (not currently configured)

## Deployment

### Docker Images

- **Backend**: Built from `Service/Api/Dockerfile` or `Dockerfile_Pipe` (for CI/CD)
- **Frontend**: Built from `Client/docker/Dockerfile` (uses multi-stage build)
- Both pushed to container registry during CD pipeline

### Production Deployment

- Uses `docker-compose.server.yml` (production-style with Traefik)
- Requires external Traefik network: `traefik-proxy`
- Environment variables injected at runtime
- Frontend image embeds environment-placeholder substitution (see `vite.config.ts`)

## Key Dependencies

### Backend
- **FastEndpoints 5.27.0**: Endpoint-centric HTTP handler pattern
- **Entity Framework Core 8**: ORM
- **Npgsql**: PostgreSQL driver
- **Docker.DotNet 3.125.15**: Docker API client
- **YamlDotNet 16.3.0**: Docker Compose YAML parsing
- **FluentValidation 11.3.0**: Request validation
- **JWT Bearer**: OAuth2 token validation

### Frontend
- **React 18.3**: UI framework
- **React Query 5.90**: Server state management
- **React Router 7.13**: Client-side routing
- **React OIDC Context 3.3**: Keycloak integration
- **Material UI 7.3**: Component library
- **Vite 7.3**: Build tool
- **openapi-typescript-codegen**: API client generation

## Known Constraints & Patterns

1. **Docker Socket Mounting**: Backend must have access to Docker socket. Ensure it's mounted correctly in docker-compose.yml.

2. **Network-Level Variables**: Stacks on the same network inherit environment variables from the network entity. Updates require redeployment.

3. **Workspace Isolation**: Each stack gets its own workspace directory. Workspaces are retained on stack deletion (for debugging).

4. **Token Expiry**: Frontend assumes token refresh is handled by OIDC context. If token expires during a long operation, the next API call will fail.

5. **MFE Sync Rollback**: If MFE sync fails after stack deployment, the entire stack is rolled back. Ensure MFE host is always available during deployments to networks with MFE plugins.

6. **Version Filtering**: Networks can restrict deployable versions via `AllowedVersionSuffixes` (e.g., only "-staging" or "-prod" suffixes). Version validation happens in `CreateStack` endpoint.

7. **API Client Regeneration**: Must be done manually after backend API changes. `npm run fetch-api` requires backend running.

## Debugging Tips

- **Backend**: Enable FastEndpoints debug logging in appsettings.Development.json (`"FastEndpoints": "Debug"`)
- **Frontend**: Check React Query devtools in browser (requires react-query-devtools; not currently installed)
- **Docker Issues**: Check logs with `docker compose logs -f apporchestrator-server` or `apporchestrator-client`
- **Database Issues**: Connect directly to PostgreSQL with credentials from .env to inspect tables
- **Keycloak**: Admin console at `http://localhost:9345/admin` (credentials in .env)
