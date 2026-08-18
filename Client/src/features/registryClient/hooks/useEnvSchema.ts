// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useQuery } from "@tanstack/react-query";
import { envSchemaQueryOptions } from "../queries";
import type { EnvSchemaField } from "../registryApiClient";

export interface UseEnvSchemaResult {
  schema: EnvSchemaField[];
  isLoading: boolean;
  error: unknown;
}

export function useEnvSchema(
  registryId: string,
  packageId: string,
  version: string,
): UseEnvSchemaResult {
  const { data, isLoading, error } = useQuery(
    envSchemaQueryOptions(registryId, packageId, version),
  );

  return {
    schema: data ?? [],
    isLoading,
    error,
  };
}
