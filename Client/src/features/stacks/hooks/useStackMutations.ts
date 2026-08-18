// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  StacksService,
  type CreateCustomStackRequest,
  type CreateStackRequest,
  type StackComposeResponse,
  type StackDetailsDTO,
  type UpdateStackComposeRequest,
  type UpdateStackRequest,
} from "@/api/orchestrator";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { stacksQueryKeys } from "@/features/stacks/queries";
import { getApiErrorMessage } from "@/utils/errorMessages";

export function useCreateStackMutation(
  onSuccess?: (data: StackDetailsDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: CreateStackRequest) =>
      StacksService.createStack(payload),
    onSuccess: (data) => {
      showToast("Stack erfolgreich deployed", "success");
      queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Deployen"), "error"),
  });
}

export function useCreateCustomStackMutation(
  onSuccess?: (data: StackDetailsDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: CreateCustomStackRequest) =>
      StacksService.createCustomStack(payload),
    onSuccess: (data) => {
      showToast("Custom Stack erfolgreich deployed", "success");
      queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all });
      onSuccess?.(data);
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Deployen"), "error"),
  });
}

export function useUpdateStackMutation(
  projectName: string,
  onSuccess?: (data: StackDetailsDTO) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: UpdateStackRequest) =>
      StacksService.updateStackEndpoint(projectName, payload),
    onSuccess: (data) => {
      showToast("Stack aktualisiert", "success");
      queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all });
      queryClient.invalidateQueries({
        queryKey: stacksQueryKeys.detail(projectName),
      });
      onSuccess?.(data);
    },
    onError: (err: unknown) => {
      showToast(getApiErrorMessage(err, "Fehler beim Aktualisieren"), "error");
    },
  });
}

export function useUpdateStackComposeMutation(
  projectName: string,
  onSuccess?: (data: StackComposeResponse) => void,
) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  return useMutation({
    mutationFn: (payload: UpdateStackComposeRequest) =>
      StacksService.updateStackComposeEndpoint(projectName, payload),
    onSuccess: (data) => {
      showToast("Stack aktualisiert", "success");
      queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all });
      queryClient.invalidateQueries({
        queryKey: stacksQueryKeys.detail(projectName),
      });
      queryClient.invalidateQueries({
        queryKey: [...stacksQueryKeys.detail(projectName), "compose"],
      });
      onSuccess?.(data);
    },
    onError: (err: unknown) => {
      showToast(getApiErrorMessage(err, "Fehler beim Aktualisieren"), "error");
    },
  });
}

export function useStackCompose(projectName: string, enabled = true) {
  return useQuery({
    queryKey: [...stacksQueryKeys.detail(projectName), "compose"],
    queryFn: () => StacksService.getCompose(projectName),
    enabled: !!projectName && enabled,
  });
}
