// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useQueries } from "@tanstack/react-query";
import { envSchemaQueryOptions } from "@/features/registryClient/queries";
import type { EnvSchemaField } from "@/features/registryClient/registryApiClient";

export interface AppWithSchema {
  registryId: string;
  packageId: string;
  version: string;
  stackName?: string;
}

/**
 * Loads env schemas for all apps in parallel using useQueries (safe for dynamic arrays).
 */
export function useMergedEnvSchemas(apps: AppWithSchema[]) {
  const results = useQueries({
    queries: apps.map((app) =>
      envSchemaQueryOptions(app.registryId, app.packageId, app.version),
    ),
  });

  const isLoading = results.some((r) => r.isLoading);
  const errorResult = results.find((r) => r.error);
  const error = errorResult
    ? errorResult.error instanceof Error
      ? errorResult.error.message
      : "Unknown error"
    : null;

  const schemas: Record<string, { schema: EnvSchemaField[]; packageId: string }> = {};
  if (!isLoading && !errorResult) {
    results.forEach((r, idx) => {
      const app = apps[idx];
      schemas[app.packageId] = {
        schema: (r.data as EnvSchemaField[] | undefined) ?? [],
        packageId: app.packageId,
      };
    });
  }

  return { schemas, isLoading, error };
}
