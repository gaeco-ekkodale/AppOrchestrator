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
import { StacksService } from "@/api/orchestrator";

export const stacksQueryKeys = {
  all: ["stacks"] as const,
  detail: (projectName: string) => ["stacks", projectName] as const,
  containers: (projectName: string) =>
    ["stacks", projectName, "containers"] as const,
};

export const stacksQueryOptions = queryOptions({
  queryKey: stacksQueryKeys.all,
  queryFn: () => StacksService.getAllStacks(),
});

export const stackQueryOptions = (projectName: string) =>
  queryOptions({
    queryKey: stacksQueryKeys.detail(projectName),
    queryFn: () => StacksService.getStack(projectName),
    enabled: !!projectName,
  });

export const containersQueryOptions = (projectName: string, enabled = true) =>
  queryOptions({
    queryKey: stacksQueryKeys.containers(projectName),
    queryFn: () => StacksService.listStackContainersEndpoint(projectName),
    enabled: !!projectName && enabled,
  });
