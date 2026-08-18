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
import type { ContainerRegistryDTO } from '../models/ContainerRegistryDTO';
import type { CreateContainerRegistryRequest } from '../models/CreateContainerRegistryRequest';
import type { TestContainerRegistryRequest } from '../models/TestContainerRegistryRequest';
import type { TestContainerRegistryResponse } from '../models/TestContainerRegistryResponse';
import type { UpdateContainerRegistryRequest } from '../models/UpdateContainerRegistryRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ContainerRegistriesService {
    /**
     * Create container registry.
     * Creates a persistent registry entry and validates credentials by executing docker login before saving. Credentials are only used for validation and are not returned in responses.
     * @param requestBody
     * @returns ContainerRegistryDTO Registry was created after successful docker authentication.
     * @throws ApiError
     */
    public static createContainerRegistry(
        requestBody: CreateContainerRegistryRequest,
    ): CancelablePromise<ContainerRegistryDTO> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/container-registries',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Validation failed or docker login was rejected by the registry.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                409: `Another registry entry already uses the same server address.`,
            },
        });
    }
    /**
     * List container registries.
     * Returns all configured container registries that can be used for image pulls. Use this endpoint to populate registry selection in deployment clients.
     * @returns ContainerRegistryDTO A list of container registry records without credential material.
     * @throws ApiError
     */
    public static getAllContainerRegistries(): CancelablePromise<Array<ContainerRegistryDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/container-registries',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Delete container registry.
     * Removes a registry definition by id. The endpoint also triggers docker logout for the stored server address as a cleanup step.
     * @param id
     * @returns void
     * @throws ApiError
     */
    public static deleteContainerRegistry(
        id: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/container-registries/{id}',
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
     * Update container registry.
     * Performs a partial update on a registry entry and revalidates access using docker logout on the old address followed by docker login on the target address.
     * @param id
     * @param requestBody
     * @returns ContainerRegistryDTO Registry was updated and returned in its final persisted state.
     * @throws ApiError
     */
    public static updateContainerRegistry(
        id: string,
        requestBody: UpdateContainerRegistryRequest,
    ): CancelablePromise<ContainerRegistryDTO> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/container-registries/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Validation failed or docker login was rejected by the registry.`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
                404: `No registry exists for the provided id.`,
                409: `Another registry entry already uses the requested server address.`,
            },
        });
    }
    /**
     * Test container registry credentials.
     * Executes docker login with the provided credentials and, on success, docker logout. No registry entity is persisted. This endpoint is intended for pre-validation in client forms.
     * @param requestBody
     * @returns TestContainerRegistryResponse Credential test result including success flag and message.
     * @throws ApiError
     */
    public static testContainerRegistry(
        requestBody: TestContainerRegistryRequest,
    ): CancelablePromise<TestContainerRegistryResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/container-registries/test',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
}
