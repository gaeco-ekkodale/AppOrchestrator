// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { queryOptions } from "@tanstack/react-query";
import {
  fetchAppsFromRegistry,
  fetchVersionsFromRegistry,
  fetchEnvSchema,
  fetchSingleAppFromRegistry,
} from "./registryApiClient";
import type { AppRegistryDTO } from "@/api/orchestrator";

export const registryAppsQueryKeys = {
  byRegistry: (registryId: string) => ["registry", "apps", registryId] as const,
  versions: (registryId: string, packageId: string) =>
    ["registry", "versions", registryId, packageId] as const,
  appDetail: (registryId: string, packageId: string) =>
    ["registry", "app", registryId, packageId] as const,
  envSchema: (registryId: string, packageId: string, version: string) =>
    ["registry", "envSchema", registryId, packageId, version] as const,
};

export const appsFromRegistryQueryOptions = (registry: AppRegistryDTO) =>
  queryOptions({
    queryKey: registryAppsQueryKeys.byRegistry(registry.id!),
    queryFn: () => fetchAppsFromRegistry(registry),
    enabled: !!registry.id,
  });

export const versionsQueryOptions = (registryId: string, packageId: string) =>
  queryOptions({
    queryKey: registryAppsQueryKeys.versions(registryId, packageId),
    queryFn: () => fetchVersionsFromRegistry(registryId, packageId),
    enabled: !!registryId && !!packageId,
  });

export const appDetailQueryOptions = (registryId: string, packageId: string) =>
  queryOptions({
    queryKey: registryAppsQueryKeys.appDetail(registryId, packageId),
    queryFn: () => fetchSingleAppFromRegistry(registryId, packageId),
    enabled: !!registryId && !!packageId,
  });

export const envSchemaQueryOptions = (
  registryId: string,
  packageId: string,
  version: string,
) =>
  queryOptions({
    queryKey: registryAppsQueryKeys.envSchema(registryId, packageId, version),
    queryFn: () => fetchEnvSchema(registryId, packageId, version),
    enabled: !!registryId && !!packageId && !!version,
    staleTime: Infinity,
  });
