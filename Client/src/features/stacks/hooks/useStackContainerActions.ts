// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { StacksService } from "@/api/orchestrator";
import { stacksQueryKeys } from "@/features/stacks/queries";
import { useToast } from "@/features/shared/contexts/ToastContext";

interface StackContainerActionInput {
  projectName: string;
  containerId: string;
}

export function useStartContainerMutation() {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: ({ projectName, containerId }: StackContainerActionInput) =>
      StacksService.startStackContainerEndpoint(projectName, containerId),
    onSuccess: async (_, variables) => {
      showToast("Container gestartet", "success");
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: stacksQueryKeys.containers(variables.projectName),
        }),
        queryClient.invalidateQueries({
          queryKey: stacksQueryKeys.detail(variables.projectName),
        }),
        queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all }),
      ]);
    },
    onError: () => {
      showToast("Container-Aktion fehlgeschlagen", "error");
    },
  });
}

export function useStopContainerMutation() {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: ({ projectName, containerId }: StackContainerActionInput) =>
      StacksService.stopStackContainerEndpoint(projectName, containerId),
    onSuccess: async (_, variables) => {
      showToast("Container gestoppt", "success");
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: stacksQueryKeys.containers(variables.projectName),
        }),
        queryClient.invalidateQueries({
          queryKey: stacksQueryKeys.detail(variables.projectName),
        }),
        queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all }),
      ]);
    },
    onError: () => {
      showToast("Container-Aktion fehlgeschlagen", "error");
    },
  });
}
