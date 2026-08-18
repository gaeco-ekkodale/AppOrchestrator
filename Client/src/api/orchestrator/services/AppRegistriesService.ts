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
import type { AppRegistryDTO } from '../models/AppRegistryDTO';
import type { CreateAppRegistryRequest } from '../models/CreateAppRegistryRequest';
import type { UpdateAppRegistryRequest } from '../models/UpdateAppRegistryRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AppRegistriesService {
    /**
     * Create an application registry.
     * Stores a new registry definition that can later be referenced during stack deployments. The base URL must be unique because it is the routing key used to fetch package metadata and compose files.
     * @param requestBody
     * @returns AppRegistryDTO The registry was created and returned with generated identifiers and timestamps.
     * @throws ApiError
     */
    public static createAppRegistry(
        requestBody: CreateAppRegistryRequest,
    ): CancelablePromise<AppRegistryDTO> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/app-registries',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `The request payload is invalid, for example missing required fields.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                409: `Another registry already uses the same base URL.`,
            },
        });
    }
    /**
     * List application registries.
     * Returns all stored registry definitions including metadata such as creation time and linked stack count. This endpoint is typically used to populate selection lists for deployment workflows.
     * @returns AppRegistryDTO A full list of registries sorted by repository implementation defaults.
     * @throws ApiError
     */
    public static getAllAppRegistries(): CancelablePromise<Array<AppRegistryDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Delete application registry.
     * Removes a registry definition from persistent storage. Existing stacks referencing the registry are subject to repository and database constraints.
     * @param id
     * @returns void
     * @throws ApiError
     */
    public static deleteAppRegistry(
        id: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/app-registries/{id}',
            path: {
                'id': id,
            },
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No registry exists for the provided id.`,
                409: `The registry is still referenced by one or more stacks.`,
            },
        });
    }
    /**
     * Get application registry by id.
     * Retrieves one registry record including current metadata. Useful for edit forms and detailed registry views.
     * @param id
     * @returns AppRegistryDTO The requested registry was found and returned.
     * @throws ApiError
     */
    public static getAppRegistry(
        id: string,
    ): CancelablePromise<AppRegistryDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries/{id}',
            path: {
                'id': id,
            },
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No registry exists for the provided id.`,
            },
        });
    }
    /**
     * Update an application registry.
     * Performs a partial update. Only fields present in the request are changed. The endpoint returns the complete resulting state after persistence.
     * @param id
     * @param requestBody
     * @returns AppRegistryDTO The registry was updated and returned in its final persisted state.
     * @throws ApiError
     */
    public static updateAppRegistry(
        id: string,
        requestBody: UpdateAppRegistryRequest,
    ): CancelablePromise<AppRegistryDTO> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/app-registries/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No registry exists for the provided id.`,
                409: `Another registry already uses the requested base URL.`,
            },
        });
    }
    /**
     * Get a single application from a registry.
     * Proxies the request to the external App Registry using the stored API key.
     * @param registryId
     * @param packageId
     * @returns any Application returned as JSON.
     * @throws ApiError
     */
    public static getRegistryApplication(
        registryId: string,
        packageId: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries/{registryId}/applications/{packageId}',
            path: {
                'registryId': registryId,
                'packageId': packageId,
            },
            errors: {
                401: `Not authenticated.`,
                403: `Forbidden`,
                404: `Registry or application not found.`,
                502: `Upstream registry error.`,
            },
        });
    }
    /**
     * List all applications from a registry.
     * Proxies the request to the external App Registry using the stored API key.
     * @param registryId
     * @returns any Applications returned as JSON.
     * @throws ApiError
     */
    public static getRegistryApplications(
        registryId: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries/{registryId}/applications',
            path: {
                'registryId': registryId,
            },
            errors: {
                401: `Not authenticated.`,
                403: `Forbidden`,
                404: `Registry not found.`,
                502: `Upstream registry error.`,
            },
        });
    }
    /**
     * Download a file from a specific version of a package.
     * Proxies the file download to the external App Registry using the stored API key. Supports docker-compose.yaml, .env.schema.yaml, icons, README.md, etc.
     * @param registryId
     * @param packageId
     * @param version
     * @param fileName
     * @returns any File content streamed.
     * @throws ApiError
     */
    public static getRegistryFile(
        registryId: string,
        packageId: string,
        version: string,
        fileName: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries/{registryId}/applications/{packageId}/versions/{version}/files/{**fileName}',
            path: {
                'registryId': registryId,
                'packageId': packageId,
                'version': version,
                '**fileName': fileName,
            },
            errors: {
                401: `Not authenticated.`,
                403: `Forbidden`,
                404: `Registry, application, version, or file not found.`,
                502: `Upstream registry error.`,
            },
        });
    }
    /**
     * Get a specific version of a package from a registry.
     * Proxies the request to the external App Registry using the stored API key.
     * @param registryId
     * @param packageId
     * @param version
     * @returns any Version returned as JSON.
     * @throws ApiError
     */
    public static getRegistryVersion(
        registryId: string,
        packageId: string,
        version: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries/{registryId}/applications/{packageId}/versions/{version}',
            path: {
                'registryId': registryId,
                'packageId': packageId,
                'version': version,
            },
            errors: {
                401: `Not authenticated.`,
                403: `Forbidden`,
                404: `Registry, application, or version not found.`,
                502: `Upstream registry error.`,
            },
        });
    }
    /**
     * List all versions of a package from a registry.
     * Proxies the request to the external App Registry using the stored API key.
     * @param registryId
     * @param packageId
     * @returns any Versions returned as JSON.
     * @throws ApiError
     */
    public static getRegistryVersions(
        registryId: string,
        packageId: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/app-registries/{registryId}/applications/{packageId}/versions',
            path: {
                'registryId': registryId,
                'packageId': packageId,
            },
            errors: {
                401: `Not authenticated.`,
                403: `Forbidden`,
                404: `Registry or application not found.`,
                502: `Upstream registry error.`,
            },
        });
    }
}
