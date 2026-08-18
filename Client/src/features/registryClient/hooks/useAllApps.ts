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
import { useRegistries } from "@/features/appRegistries/hooks/useRegistries";
import { appsFromRegistryQueryOptions } from "../queries";
import type { AppWithRegistry } from "../registryApiClient";

/**
 * Fetches apps from ALL orchestrator-registered registries in parallel.
 * Each registry's baseUrl is used as the target for the registry API calls.
 */
export function useAllApps(): {
  apps: AppWithRegistry[];
  isLoading: boolean;
  errors: unknown[];
  registryCount: number;
} {
  const { registries, isLoading: regLoading } = useRegistries();

  const results = useQueries({
    queries: registries.map((registry) =>
      appsFromRegistryQueryOptions(registry),
    ),
  });

  const isLoading = regLoading || results.some((r) => r.isLoading);
  const apps = results.flatMap((r) => r.data ?? []);
  const errors = results.map((r) => r.error).filter(Boolean);

  return { apps, isLoading, errors, registryCount: registries.length };
}
