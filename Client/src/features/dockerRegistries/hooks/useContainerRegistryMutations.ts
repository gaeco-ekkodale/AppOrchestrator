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
  ContainerRegistriesService,
  type ContainerRegistryDTO,
  type CreateContainerRegistryRequest,
  type TestContainerRegistryRequest,
  type TestContainerRegistryResponse,
  type UpdateContainerRegistryRequest,
} from "@/api/orchestrator";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { containerRegistriesQueryKeys } from "@/features/dockerRegistries/queries";
import { getApiErrorMessage } from "@/utils/errorMessages";

export function useCreateContainerRegistryMutation(
  onSuccess?: (data: ContainerRegistryDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: CreateContainerRegistryRequest) =>
      ContainerRegistriesService.createContainerRegistry(payload),
    onSuccess: (data) => {
      showToast("Registry hinzugefügt und docker login ausgeführt", "success");
      queryClient.invalidateQueries({
        queryKey: containerRegistriesQueryKeys.all,
      });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Hinzufügen"), "error"),
  });
}

export function useUpdateContainerRegistryMutation(
  id: string,
  onSuccess?: (data: ContainerRegistryDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: UpdateContainerRegistryRequest) =>
      ContainerRegistriesService.updateContainerRegistry(id, payload),
    onSuccess: (data) => {
      showToast("Registry aktualisiert und docker login erneuert", "success");
      queryClient.invalidateQueries({
        queryKey: containerRegistriesQueryKeys.all,
      });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Aktualisieren"), "error"),
  });
}

export function useDeleteContainerRegistryMutation(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (id: string) =>
      ContainerRegistriesService.deleteContainerRegistry(id),
    onSuccess: () => {
      showToast(
        "Container-Registry entfernt und docker logout ausgeführt",
        "success",
      );
      queryClient.invalidateQueries({
        queryKey: containerRegistriesQueryKeys.all,
      });
      onSuccess?.();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Entfernen"), "error"),
  });
}

export function useTestContainerRegistryMutation(
  onSuccess?: (result: TestContainerRegistryResponse) => void,
  onError?: (message: string) => void,
) {
  return useMutation({
    mutationFn: (payload: TestContainerRegistryRequest) =>
      ContainerRegistriesService.testContainerRegistry(payload),
    onSuccess: (data) => {
      onSuccess?.(data);
    },
    onError: (err: unknown) => {
      onError?.(getApiErrorMessage(err, "Fehler beim Testen"));
    },
  });
}
