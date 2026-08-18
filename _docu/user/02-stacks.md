# Managing Stacks

A **Stack** is a Docker Compose project managed by the App Orchestrator. There are two types of stacks:

- **Registry Stack**: Deployed from a versioned package in a connected App Registry. Version updates are managed via the Orchestrator UI.
- **Custom Stack**: Deployed from a user-supplied `docker-compose.yml`. The compose file can be edited directly.

## Viewing Stacks

Navigate to **Stacks** in the navigation bar. The list shows all managed stacks together with their live Docker status (e.g., Running, Stopped, Partial).

Stacks discovered in Docker that have no database record are also shown, labeled as **External**.

## Deploying a Stack from the App Store

1. Navigate to **Store** in the navigation bar.
2. Browse available packages from the connected App Registries.
3. Click on a package version and select **Deploy**.
4. Fill in the environment configuration form (all required variables from the `.env.schema.yaml` will be presented).
5. Optionally assign the stack to a **Network** (Environment).
6. Click **Deploy** to start the stack.

Alternatively, use **Stacks → Deploy** and manually provide the Registry, Package ID, Version, and environment variables.

## Deploying a Custom Stack

Use the API endpoint `POST /api/stacks/custom` with a user-supplied `docker-compose.yml` content to create a custom stack.

## Stack Lifecycle

From the stack detail page (`/stacks/:id`) you can perform the following operations:

| Action             | Description                                                                            |
| :----------------- | :------------------------------------------------------------------------------------- |
| **Start**          | Starts all containers in the stack (`docker compose up -d`).                           |
| **Stop**           | Stops all containers in the stack (`docker compose down`).                             |
| **Restart**        | Stops and then starts the stack.                                                       |
| **Update Version** | _(Registry Stacks only)_ Fetches the new compose file from the registry and redeploys. |
| **Edit Compose**   | _(Custom Stacks only)_ Edit the `docker-compose.yml` content directly in the UI.       |
| **Clone**          | Creates a copy of the stack with a new name.                                           |
| **Delete**         | Stops the stack and removes all workspace files. This action is irreversible.          |

## Updating a Registry Stack

To update a Registry Stack to a new version:

1. Open the stack detail page.
2. Enter the new version number in the **Update Version** field.
3. Confirm – the orchestrator will fetch the new compose file from the registry, back up the current compose files, and redeploy.

The number of backups retained is configurable via `Orchestrator:VersionUpdateBackupRetention` (default: 5).

## Viewing Container Status

The stack detail page shows the live status of each container within the stack, including state, ports, and image.
