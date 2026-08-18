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
  AppRegistriesService,
  type AppRegistryDTO,
  type CreateAppRegistryRequest,
  type UpdateAppRegistryRequest,
} from "@/api/orchestrator";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { registriesQueryKeys } from "@/features/appRegistries/queries";
import { getApiErrorMessage } from "@/utils/errorMessages";

export function useCreateAppRegistryMutation(
  onSuccess?: (data: AppRegistryDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: CreateAppRegistryRequest) =>
      AppRegistriesService.createAppRegistry(payload),
    onSuccess: (data) => {
      showToast("Registry erstellt", "success");
      queryClient.invalidateQueries({ queryKey: registriesQueryKeys.all });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Erstellen"), "error"),
  });
}

export function useUpdateAppRegistryMutation(
  id: string,
  onSuccess?: (data: AppRegistryDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: UpdateAppRegistryRequest) =>
      AppRegistriesService.updateAppRegistry(id, payload),
    onSuccess: (data) => {
      showToast("Registry aktualisiert", "success");
      queryClient.invalidateQueries({ queryKey: registriesQueryKeys.all });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Aktualisieren"), "error"),
  });
}

export function useDeleteAppRegistryMutation(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (id: string) => AppRegistriesService.deleteAppRegistry(id),
    onSuccess: () => {
      showToast("Registry gelöscht", "success");
      queryClient.invalidateQueries({ queryKey: registriesQueryKeys.all });
      onSuccess?.();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Löschen"), "error"),
  });
}
