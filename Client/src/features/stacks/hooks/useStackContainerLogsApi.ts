// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback } from "react";
import {
  StacksService,
  type ContainerLogsResponseDTO,
} from "@/api/orchestrator";

interface FetchContainerLogsInput {
  projectName: string;
  containerId: string;
  since?: string | null;
  tail?: number;
  limit?: number;
}

export function useStackContainerLogsApi() {
  const fetchContainerLogs = useCallback(
    ({
      projectName,
      containerId,
      since = null,
      tail,
      limit,
    }: FetchContainerLogsInput): Promise<ContainerLogsResponseDTO> => {
      return StacksService.getStackContainerLogsEndpoint(
        projectName,
        containerId,
        since,
        tail,
        limit,
      );
    },
    [],
  );

  return { fetchContainerLogs };
}
