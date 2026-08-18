# API Reference

This reference describes the most important endpoints of the App Orchestrator API.
The complete, interactive documentation (Swagger UI) can be found at `http://localhost:6241/swagger`.

**Base URL:** `http://localhost:6241`
**Route Prefix:** All endpoints are prefixed with `/api`
**Authentication:** All endpoints require a Bearer Token: `Authorization: Bearer <token>`

---

## Stacks

### `GET /api/stacks`

Lists all stacks, combining persisted database entries with live Docker status. Also includes Docker-discovered stacks with no database record (reported as `External` source).

**Response (200 OK):** `Array of StackDTO`

### `POST /api/stacks`

Deploys a new Registry Stack from a package in a connected App Registry.

**Request Body:** `application/json`

```json
{
  "stackName": "my-stack",
  "registryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "packageId": "com.example.my-app",
  "version": "1.0.0",
  "envConfig": {
    "DATABASE_HOST": "db",
    "API_KEY": "secret"
  },
  "networkName": "gaeco-local"
}
```

**Response (200 OK):** `StackDTO`

### `GET /api/stacks/{projectName}`

Retrieves details for a specific stack by its Docker Compose project name.

**Response (200 OK):** `StackDTO`

### `PUT /api/stacks/{projectName}`

Partially updates a stack (rename, version update, env config update, network reassignment). Supports partial updates – only include fields you want to change.

**Request Body:** `application/json`

```json
{
  "stackName": "new-name",
  "version": "1.1.0",
  "envConfig": {"API_KEY": "new-secret"},
  "networkName": "other-network"
}
```

**Response (200 OK):** `StackDTO`

### `DELETE /api/stacks/{projectName}`

Stops the stack and removes all workspace files.

**Response (204 No Content)**

### `POST /api/stacks/{projectName}/start`

Starts all containers in the stack.

**Response (200 OK):** `StackDTO`

### `POST /api/stacks/{projectName}/stop`

Stops all containers in the stack.

**Response (200 OK):** `StackDTO`

### `POST /api/stacks/{projectName}/restart`

Restarts all containers in the stack.

**Response (200 OK):** `StackDTO`

### `POST /api/stacks/{projectName}/clone`

Creates a copy of the stack with a new name.

**Request Body:** `application/json`

```json
{
  "newStackName": "my-stack-copy"
}
```

**Response (200 OK):** `StackDTO`

### `GET /api/stacks/{projectName}/compose`

Returns the current `docker-compose.yml` content of the stack.

**Response (200 OK):** `string` (YAML content)

### `PUT /api/stacks/{projectName}/compose`

Updates the `docker-compose.yml` of a Custom Stack and redeploys it.

**Request Body:** `application/json`

```json
{
  "content": "version: '3'\\nservices: ..."
}
```

**Response (200 OK):** `StackDTO`

---

## Networks

### `GET /api/networks`

Lists all managed networks.

**Response (200 OK):** `Array of NetworkDTO`

### `POST /api/networks`

Creates a new Docker network and persists it.

**Request Body:** `application/json`

```json
{
  "name": "my-network",
  "environmentVariables": [{"name": "DB_HOST", "value": "postgres"}]
}
```

**Response (201 Created):** `NetworkDTO`

### `GET /api/networks/{name}`

Retrieves details for a specific network.

**Response (200 OK):** `NetworkDTO`

### `PUT /api/networks/{name}`

Updates a network's shared environment variables.

**Response (200 OK):** `NetworkDTO`

### `DELETE /api/networks/{name}`

Removes the network from the database and from Docker.

**Response (204 No Content)**

---

## App Registries

### `GET /api/registries`

Lists all connected App Registry instances.

**Response (200 OK):** `Array of AppRegistryDTO`

### `POST /api/registries`

Adds a new App Registry connection.

**Request Body:** `application/json`

```json
{
  "name": "Main Registry",
  "baseUrl": "https://registry.example.com"
}
```

**Response (201 Created):** `AppRegistryDTO`

### `GET /api/registries/{id}`

Retrieves details for a specific App Registry.

### `PUT /api/registries/{id}`

Updates an App Registry entry.

### `DELETE /api/registries/{id}`

Removes an App Registry connection.

---

## Container Registries

### `GET /api/container-registries`

Lists all configured container image registries.

**Response (200 OK):** `Array of ContainerRegistryDTO`

### `POST /api/container-registries`

Adds a new container registry and runs `docker login`.

**Request Body:** `application/json`

```json
{
  "name": "Azure CR",
  "serverAddress": "myregistry.azurecr.io",
  "username": "myuser",
  "password": "mypassword"
}
```

**Response (201 Created):** `ContainerRegistryDTO`

### `GET /api/container-registries/{id}`

Retrieves details for a specific container registry.

### `PUT /api/container-registries/{id}`

Updates a container registry entry and re-authenticates.

### `DELETE /api/container-registries/{id}`

Removes a container registry entry and runs `docker logout`.

---

## Common Status Codes

| Code                 | Meaning                                                             |
| :------------------- | :------------------------------------------------------------------ |
| **200 OK**           | Request successful.                                                 |
| **201 Created**      | Resource created successfully.                                      |
| **204 No Content**   | Resource deleted successfully.                                      |
| **400 Bad Request**  | Validation error. Check the error message in the response body.     |
| **401 Unauthorized** | Missing or invalid Bearer Token.                                    |
| **404 Not Found**    | The requested resource does not exist.                              |
| **409 Conflict**     | A resource with the same name/ID already exists.                    |
| **502 Bad Gateway**  | An upstream service (Docker daemon, App Registry) is not reachable. |
