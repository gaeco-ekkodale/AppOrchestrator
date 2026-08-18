// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Hand-written service for endpoints not yet included in the generated StacksService.
 * Run `npm run fetch-api` after the backend is updated to generate the official version.
 */
import type { StackDTO } from "./models/StackDTO";
import type { CancelablePromise } from "./core/CancelablePromise";
import { OpenAPI } from "./core/OpenAPI";
import { request as __request } from "./core/request";

export interface DeployCustomRequest {
  stackName: string;
  composeContent: string;
  envConfig?: Record<string, string>;
}

export class StacksCustomService {
  /**
   * Deploys a new stack from a raw docker-compose.yml string (no AppRegistry required).
   */
  public static deployCustomEndpoint(
    requestBody: DeployCustomRequest,
  ): CancelablePromise<StackDTO> {
    return __request(OpenAPI, {
      method: "POST",
      url: "/api/stacks/custom",
      body: requestBody,
      mediaType: "application/json",
      errors: {
        400: "Validation error.",
        401: "Not authenticated.",
        409: "A stack with this name already exists.",
      },
    });
  }
}
