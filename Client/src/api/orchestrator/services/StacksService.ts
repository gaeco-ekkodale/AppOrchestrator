// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CloneStackRequest } from '../models/CloneStackRequest';
import type { ContainerDTO } from '../models/ContainerDTO';
import type { ContainerLogsResponseDTO } from '../models/ContainerLogsResponseDTO';
import type { CreateCustomStackRequest } from '../models/CreateCustomStackRequest';
import type { CreateStackRequest } from '../models/CreateStackRequest';
import type { StackComposeResponse } from '../models/StackComposeResponse';
import type { StackDetailsDTO } from '../models/StackDetailsDTO';
import type { StackDTO } from '../models/StackDTO';
import type { UpdateStackComposeRequest } from '../models/UpdateStackComposeRequest';
import type { UpdateStackRequest } from '../models/UpdateStackRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class StacksService {
    /**
     * Clone stack metadata and workspace.
     * Copies the full workspace of an existing stack - compose, env, package files and volume data - into a new workspace and creates a new stack record. Since the project name is derived from stack name and network, at least one of them must differ from the source. No docker command is executed during cloning.
     * @param projectName
     * @param requestBody
     * @returns StackDTO Clone was created successfully.
     * @throws ApiError
     */
    public static cloneStack(
        projectName: string,
        requestBody: CloneStackRequest,
    ): CancelablePromise<StackDTO> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/clone',
            path: {
                'projectName': projectName,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Neither name nor network differs from the source.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Source managed stack does not exist for the provided project name.`,
                409: `Target stack name already exists on the target network.`,
            },
        });
    }
    /**
     * Deploy stack from custom compose.
     * Deploys a stack from raw docker-compose content supplied by the client. The API stores stack metadata with no linked application registry.
     * @param requestBody
     * @returns StackDTO Stack was deployed and persisted successfully.
     * @throws ApiError
     */
    public static createCustomStack(
        requestBody: CreateCustomStackRequest,
    ): CancelablePromise<StackDTO> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/custom',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Request payload validation failed.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                409: `Another stack already uses the same derived project name.`,
                502: `The plugin host in the target network is currently unavailable.`,
            },
        });
    }
    /**
     * Deploy stack from registry package.
     * Creates a new stack from a package version in an application registry. The endpoint fetches compose content, writes workspace files, executes docker compose up, and persists stack metadata.
     * @param requestBody
     * @returns StackDTO Stack was deployed and persisted successfully.
     * @throws ApiError
     */
    public static createStack(
        requestBody: CreateStackRequest,
    ): CancelablePromise<StackDTO> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Request payload validation failed.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Referenced registry does not exist.`,
                409: `Another stack already uses the same derived project name.`,
                502: `Required upstream services (registry or plugin host) are currently unavailable.`,
            },
        });
    }
    /**
     * List stacks.
     * Returns persisted stacks enriched with live Docker status. Additionally includes Docker-discovered stacks that have no database record, reported as External source.
     * @returns StackDTO Stack list including persisted metadata and live status.
     * @throws ApiError
     */
    public static getAllStacks(): CancelablePromise<Array<StackDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/stacks',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Delete stack.
     * Stops and removes all Docker containers and networks for the compose project, deletes the workspace directory, and removes the database record if one exists.
     * @param projectName
     * @returns void
     * @throws ApiError
     */
    public static deleteStack(
        projectName: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/stacks/{projectName}',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter projectName is invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                500: `Docker returned an error while removing containers.`,
            },
        });
    }
    /**
     * Get stack by project name.
     * Loads one stack by project name, resolves current runtime status and container list from Docker, and returns parsed env configuration from the workspace .env file. Falls back to Docker-discovered external stacks when no managed DB entry exists.
     * @param projectName
     * @returns StackDetailsDTO Detailed stack payload including env key-value pairs and containers.
     * @throws ApiError
     */
    public static getStack(
        projectName: string,
    ): CancelablePromise<StackDetailsDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/stacks/{projectName}',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter projectName is invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No stack exists for the provided project name.`,
            },
        });
    }
    /**
     * Update stack.
     * Supports partial updates for a stack identified by docker project name. Rename requires a stopped stack. Version updates fetch a new compose file from the linked registry and apply it with backup handling.
     * @param projectName
     * @param requestBody
     * @returns StackDTO Stack was updated and returned with current runtime status.
     * @throws ApiError
     */
    public static updateStackEndpoint(
        projectName: string,
        requestBody: UpdateStackRequest,
    ): CancelablePromise<StackDTO> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/stacks/{projectName}',
            path: {
                'projectName': projectName,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `No supported fields were provided or a custom stack attempted a version update.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Stack or linked registry was not found.`,
                409: `Rename conflict occurred or stack must be stopped before renaming.`,
                502: `Linked upstream services are currently unavailable.`,
            },
        });
    }
    /**
     * Delete stack volumes.
     * Removes all Docker volumes associated with the given compose project.
     * @param projectName
     * @returns void
     * @throws ApiError
     */
    public static deleteStackVolumes(
        projectName: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/stacks/{projectName}/volumes',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter projectName is invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                500: `An error occurred while removing the volumes.`,
            },
        });
    }
    /**
     * Get compose and env for stack.
     * Loads the editable docker-compose.yml and .env content from the workspace of a custom stack identified by docker project name.
     * @param projectName
     * @returns StackComposeResponse Compose and env content for the stack.
     * @throws ApiError
     */
    public static getCompose(
        projectName: string,
    ): CancelablePromise<StackComposeResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/stacks/{projectName}/compose',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter is invalid, or the stack is registry-managed and does not support direct compose editing.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No managed stack exists for the provided project name.`,
            },
        });
    }
    /**
     * Update compose and env for stack.
     * Writes a new docker-compose.yml and .env for a custom stack identified by docker project name, then executes docker compose up to apply the changes.
     * @param projectName
     * @param requestBody
     * @returns StackComposeResponse Compose and env were updated and the latest persisted content is returned.
     * @throws ApiError
     */
    public static updateStackComposeEndpoint(
        projectName: string,
        requestBody: UpdateStackComposeRequest,
    ): CancelablePromise<StackComposeResponse> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/stacks/{projectName}/compose',
            path: {
                'projectName': projectName,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Invalid payload or stack is registry-managed.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No managed stack exists for the provided project name.`,
                500: `The compose update could not be applied. Please verify compose and environment values.`,
            },
        });
    }
    /**
     * Restart stack.
     * Restarts all containers for the Docker Compose project identified by projectName.
     * @param projectName
     * @returns void
     * @throws ApiError
     */
    public static restartStack(
        projectName: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/restart',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter projectName is invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                500: `Docker returned an error while restarting containers.`,
            },
        });
    }
    /**
     * Start stack.
     * Starts all stopped containers for the compose project identified by projectName. When no containers exist yet, falls back to docker compose up -d using the persisted workspace definition.
     * @param projectName
     * @returns void
     * @throws ApiError
     */
    public static startStack(
        projectName: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/start',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter projectName is invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                500: `Docker returned an error while starting containers.`,
            },
        });
    }
    /**
     * Stop stack.
     * Stops all running containers for the compose project identified by projectName without removing them.
     * @param projectName
     * @returns void
     * @throws ApiError
     */
    public static stopStack(
        projectName: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/stop',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter projectName is invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                500: `Docker returned an error while stopping containers.`,
            },
        });
    }
    /**
     * Get container by id.
     * Returns one container from a stack by matching short/full id or container name.
     * @param projectName
     * @param containerId
     * @returns ContainerDTO Container found.
     * @throws ApiError
     */
    public static getStackContainerEndpoint(
        projectName: string,
        containerId: string,
    ): CancelablePromise<ContainerDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/stacks/{projectName}/containers/{containerId}',
            path: {
                'projectName': projectName,
                'containerId': containerId,
            },
            errors: {
                400: `Bad Request`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Container was not found in the specified stack.`,
            },
        });
    }
    /**
     * Get container logs.
     * Returns log lines for a container with cursor-based incremental polling. Use 'since' from the previous response as the next request cursor.
     * @param projectName
     * @param containerId
     * @param since
     * @param tail
     * @param limit
     * @returns ContainerLogsResponseDTO Container logs response with next cursor.
     * @throws ApiError
     */
    public static getStackContainerLogsEndpoint(
        projectName: string,
        containerId: string,
        since?: string | null,
        tail?: number | null,
        limit?: number | null,
    ): CancelablePromise<ContainerLogsResponseDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/stacks/{projectName}/containers/{containerId}/logs',
            path: {
                'projectName': projectName,
                'containerId': containerId,
            },
            query: {
                'since': since,
                'tail': tail,
                'limit': limit,
            },
            errors: {
                400: `Route parameters are missing or query values are invalid.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Container was not found in the specified stack.`,
            },
        });
    }
    /**
     * List containers for a stack.
     * Queries the Docker Engine API for all containers (including stopped) that belong to the compose project identified by route id (docker project name).
     * @param projectName
     * @returns ContainerDTO List of containers with state, status and port information.
     * @throws ApiError
     */
    public static listStackContainersEndpoint(
        projectName: string,
    ): CancelablePromise<Array<ContainerDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/stacks/{projectName}/containers',
            path: {
                'projectName': projectName,
            },
            errors: {
                400: `Route parameter id is missing.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Restart container.
     * Stops then starts a single container that belongs to the given stack.
     * @param projectName
     * @param containerId
     * @returns void
     * @throws ApiError
     */
    public static restartStackContainerEndpoint(
        projectName: string,
        containerId: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/containers/{containerId}/restart',
            path: {
                'projectName': projectName,
                'containerId': containerId,
            },
            errors: {
                400: `Route parameters are missing.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Start container.
     * Starts a single Docker container that belongs to the compose project identified by projectName.
     * @param projectName
     * @param containerId
     * @returns void
     * @throws ApiError
     */
    public static startStackContainerEndpoint(
        projectName: string,
        containerId: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/containers/{containerId}/start',
            path: {
                'projectName': projectName,
                'containerId': containerId,
            },
            errors: {
                400: `Route parameters are missing.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Container was not found in the specified stack.`,
            },
        });
    }
    /**
     * Stop container.
     * Stops a single Docker container that belongs to the compose project identified by projectName.
     * @param projectName
     * @param containerId
     * @returns void
     * @throws ApiError
     */
    public static stopStackContainerEndpoint(
        projectName: string,
        containerId: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/stacks/{projectName}/containers/{containerId}/stop',
            path: {
                'projectName': projectName,
                'containerId': containerId,
            },
            errors: {
                400: `Route parameters are missing.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Container was not found in the specified stack.`,
            },
        });
    }
}
