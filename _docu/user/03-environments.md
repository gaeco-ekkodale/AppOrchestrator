# Environments (Networks)

An **Environment** in the App Orchestrator corresponds to a **Docker network**. Environments are used to group stacks that belong together and to share common configuration (environment variables) across all stacks within a group.

## Why Environments?

When multiple stacks need to communicate with each other (e.g., a backend and a frontend), they must be on the same Docker network. The Orchestrator manages this by letting you define named environments.

Additionally, you can define **shared environment variables** on an environment. These variables are automatically injected into the `.env` file of every stack deployed on that network – avoiding repetitive configuration.

## Viewing Environments

Navigate to **Environments** in the navigation bar to see all managed networks.

## Creating an Environment

1. Click **New Environment**.
2. Enter a **Name** (used as the Docker network name). The name must start with a letter or digit and may only contain alphanumeric characters, underscores, dots, and hyphens.
3. Optionally add **Shared Environment Variables** (key-value pairs).
4. Click **Create**.

The Docker network is created immediately on the host.

## Editing an Environment

You can update the shared environment variables of an existing environment at any time.

> **Note:** The environment name (Docker network name) is immutable after creation. To rename, delete and recreate the environment.

## Deleting an Environment

An environment can be deleted via the API (`DELETE /api/networks/{name}`). The corresponding Docker network will be removed from the host.

> **Warning:** Deleting an environment while stacks are still assigned to it may leave those stacks without a valid network configuration.

## Assigning a Stack to an Environment

When deploying a new stack, select the desired environment in the **Network** field. An existing stack can be reassigned to a different environment via the **Update Stack** operation.
