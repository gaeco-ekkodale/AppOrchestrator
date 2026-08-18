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
import { containersQueryOptions } from "@/features/stacks/queries";

/**
 * Fetch the container list for a single stack.
 * Pass `enabled = false` to skip the request until the user expands the row.
 */
export function useStackContainers(projectName: string, enabled = true) {
  const { data, isLoading, error } = useQuery(
    containersQueryOptions(projectName, enabled),
  );
  return { containers: data ?? [], isLoading, error };
}
