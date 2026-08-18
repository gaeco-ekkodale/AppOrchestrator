# Frontend Architecture

The frontend is built with **React**, **TypeScript**, and **Vite**. It uses **Material UI** for components and **React Query** for data fetching.

## Project Structure

- **`src/api/`**: Generated API clients (one for the Orchestrator API, one for the AppRegistry API).
- **`src/features/`**: Feature-based modules with components, hooks, and utilities scoped to a domain.
- **`src/pages/`**: Top-level page components for routing.
- **`src/layout/`**: Application shell and navigation components.
- **`src/theme/`**: Material UI theme configuration.
- **`src/utils/`**: Helper functions and utilities.
- **`src/App.tsx`**: Main application component and routing setup.

## Pages & Routing

| Route                                              | Page                        | Description                                          |
| :------------------------------------------------- | :-------------------------- | :--------------------------------------------------- |
| `/stacks`                                          | `StacksPage`                | Overview of all managed Docker stacks.               |
| `/stacks/deploy`                                   | `DeployStackPage`           | Deploy a new stack from a registry package.          |
| `/stacks/:id`                                      | `StackDetailPage`           | Detail view and management for a specific stack.     |
| `/store`                                           | `AppStorePage`              | Browse available packages from connected registries. |
| `/store/configure/:registryId/:packageId/:version` | `DeployFromStorePage`       | Configure and deploy a package from the app store.   |
| `/registries`                                      | `RegistriesPage`            | Manage connected AppRegistry instances.              |
| `/registries/new`, `/registries/:id/edit`          | `RegistryFormPage`          | Create or edit an AppRegistry connection.            |
| `/container-registries`                            | `ContainerRegistriesPage`   | Manage Docker container image registries.            |
| `/container-registries/new`, `/.../:id/edit`       | `ContainerRegistryFormPage` | Create or edit a container registry connection.      |
| `/environments`                                    | `EnvironmentsPage`          | Manage Docker networks (environments).               |

## Key Technologies

- **Vite**: Build tool and dev server.
- **React Router**: Client-side routing.
- **React Query**: Server state management and caching.
- **Material UI**: Component library.
- **Module Federation**: Used for Micro-Frontend integration (configured in `vite.config.ts`).

## Micro-Frontends

The application itself is designed as a micro-frontend so that it can be integrated into a host shell (PluginHost). The **vite-plugin-federation** is used to expose and consume modules for this integration.

The frontend connects to **two** API clients simultaneously:

1. **Orchestrator API** (`OrchestratorOpenAPI`): The AppOrchestrator backend.
2. **Registry API** (`RegistryOpenAPI`): An AppRegistry instance for browsing available packages in the App Store.

Both API clients receive the Keycloak access token automatically on every authenticated request.
