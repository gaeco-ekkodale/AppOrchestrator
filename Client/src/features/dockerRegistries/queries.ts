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
import { ContainerRegistriesService } from "@/api/orchestrator";

export const containerRegistriesQueryKeys = {
  all: ["container-registries"] as const,
};

export const containerRegistriesQueryOptions = queryOptions({
  queryKey: containerRegistriesQueryKeys.all,
  queryFn: () => ContainerRegistriesService.getAllContainerRegistries(),
});
