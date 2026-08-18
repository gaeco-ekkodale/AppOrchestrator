# Registries

The App Orchestrator supports two types of registries:

1. **App Registries**: Sources for deployable application packages (e.g., an AppRegistry instance).
2. **Container Registries**: Docker image registries for pulling private images (e.g., Azure Container Registry, Docker Hub private repos).

---

## App Registries

An **App Registry** is a connected AppRegistry API instance that the orchestrator can pull packages from. It is used to browse available applications in the **App Store** and to deploy Registry Stacks.

### Viewing App Registries

Navigate to **Registries** in the navigation bar.

### Adding an App Registry

1. Navigate to **Registries → New Registry**.
2. Enter a **Name** (display label).
3. Enter the **Base URL** of the AppRegistry API (e.g., `https://registry.example.com`).
4. Click **Save**.

### Editing / Deleting an App Registry

From the Registries list, use the edit or delete actions on any entry.

> **Note:** Deleting an App Registry does **not** stop or delete any stacks that were deployed from it. Those stacks remain but lose their registry link.

---

## Container Registries

A **Container Registry** stores Docker images. If your application packages reference images from a private registry, the orchestrator must authenticate with that registry before pulling images.

Credentials are **not stored** in the database – they are passed once during registration and stored in the Docker credential store on the host.

### Adding a Container Registry

1. Navigate to **Container Registries → New Container Registry**.
2. Enter a **Name** (display label).
3. Enter the **Server Address** (e.g., `myregistry.azurecr.io`).
4. Enter your **Username** and **Password** (or access token).
5. Click **Save**.

The orchestrator will immediately run `docker login` on the host with the provided credentials. The credentials are then managed by Docker's credential store.

### Editing / Deleting a Container Registry

From the Container Registries list, use the edit or delete actions on any entry. Deleting a registry entry runs `docker logout` for the corresponding server address.
