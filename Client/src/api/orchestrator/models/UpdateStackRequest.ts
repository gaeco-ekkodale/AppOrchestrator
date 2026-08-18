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
import type { StackRouteParams } from './StackRouteParams';
export type UpdateStackRequest = (StackRouteParams & {
    stackName?: string | null;
    version?: string | null;
    envConfig?: Record<string, string> | null;
    networkName?: string | null;
});

