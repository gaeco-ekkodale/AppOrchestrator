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
import type { CreateNetworkRequest } from '../models/CreateNetworkRequest';
import type { NetworkDTO } from '../models/NetworkDTO';
import type { UpdateNetworkRequest } from '../models/UpdateNetworkRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class NetworksService {
    /**
     * Create network.
     * Creates a Docker bridge network with the given name and saves it to the database. The name is immutable and serves as the stable identifier in both Docker and the database.
     * @param requestBody
     * @returns NetworkDTO Network created in Docker and persisted.
     * @throws ApiError
     */
    public static createNetwork(
        requestBody: CreateNetworkRequest,
    ): CancelablePromise<NetworkDTO> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/networks',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Validation failed.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                409: `A network with this name already exists.`,
            },
        });
    }
    /**
     * List networks.
     * Returns all user-created Docker networks stored in the database, including a summary of stacks assigned to each network.
     * @returns NetworkDTO List of networks.
     * @throws ApiError
     */
    public static listNetworks(): CancelablePromise<Array<NetworkDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/networks',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Delete network.
     * Removes the Docker network from the daemon by name and deletes the database record. If the network no longer exists in Docker a warning is logged and the DB record is still removed.
     * @param name
     * @returns void
     * @throws ApiError
     */
    public static deleteNetwork(
        name: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/networks/{name}',
            path: {
                'name': name,
            },
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Network not found in database.`,
                409: `Network still has containers attached and cannot be deleted.`,
            },
        });
    }
    /**
     * Get network.
     * Returns the network with the specified name, including its assigned stack summaries.
     * @param name
     * @returns NetworkDTO Network found.
     * @throws ApiError
     */
    public static getNetwork(
        name: string,
    ): CancelablePromise<NetworkDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/networks/{name}',
            path: {
                'name': name,
            },
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Network not found.`,
            },
        });
    }
    /**
     * Update network.
     * Replaces the shared environment variables for the network identified by name. All stacks deployed on this network will receive the updated variables on their next compose up.
     * @param name
     * @param requestBody
     * @returns NetworkDTO Network updated.
     * @throws ApiError
     */
    public static updateNetwork(
        name: string,
        requestBody: UpdateNetworkRequest,
    ): CancelablePromise<NetworkDTO> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/networks/{name}',
            path: {
                'name': name,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Validation failed.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `Network not found.`,
            },
        });
    }
}
