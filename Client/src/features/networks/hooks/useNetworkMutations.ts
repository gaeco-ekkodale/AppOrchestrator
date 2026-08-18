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
import {
  type EnvironmentVariableInput,
  NetworksService,
  type CreateNetworkRequest,
  type NetworkDTO,
} from "@/api/orchestrator";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { networksQueryKeys } from "@/features/networks/queries";
import { getApiErrorMessage } from "@/utils/errorMessages";

export function useCreateNetworkMutation(
  onSuccess?: (data: NetworkDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: CreateNetworkRequest) =>
      NetworksService.createNetwork(payload),
    onSuccess: (data) => {
      showToast("Environment erstellt", "success");
      queryClient.invalidateQueries({ queryKey: networksQueryKeys.all });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Erstellen"), "error"),
  });
}

export function useDeleteNetworkMutation(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (name: string) => NetworksService.deleteNetwork(name),
    onSuccess: () => {
      showToast("Environment gelöscht", "success");
      queryClient.invalidateQueries({ queryKey: networksQueryKeys.all });
      onSuccess?.();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Löschen"), "error"),
  });
}

export function useUpdateNetworkMutation(
  onSuccess?: (data: NetworkDTO) => void,
  successMessage = "Environment gespeichert",
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: ({
      name,
      environmentVariables,
      allowedVersionSuffixes,
    }: {
      name: string;
      environmentVariables?: EnvironmentVariableInput[];
      allowedVersionSuffixes?: string[];
    }) =>
      NetworksService.updateNetwork(name, {
        environmentVariables,
        allowedVersionSuffixes,
      }),
    onSuccess: (data) => {
      showToast(successMessage, "success");
      queryClient.invalidateQueries({ queryKey: networksQueryKeys.all });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Speichern"), "error"),
  });
}
