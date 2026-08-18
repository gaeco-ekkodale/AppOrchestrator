// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { StacksService } from "@/api/orchestrator";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { stacksQueryKeys } from "@/features/stacks/queries";
import { createRoute } from "@/utils/routing";
import { getApiErrorMessage } from "@/utils/errorMessages";

/**
 * Shared stack lifecycle mutations (start / stop / restart / delete / clone).
 * Used by both StacksPage (list) and StackDetailPage (detail).
 */
export function useStackActions(projectName?: string) {
  const {showToast} = useToast();
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [busyIds, setBusyIds] = useState<Set<string>>(new Set());
  const addBusy = (id: string) => setBusyIds((prev) => new Set(prev).add(id));
  const removeBusy = (id: string) =>
    setBusyIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: stacksQueryKeys.all });
    if (projectName)
      queryClient.invalidateQueries({
        queryKey: stacksQueryKeys.detail(projectName),
      });
  };

  const startMutation = useMutation({
    mutationFn: (id: string) => StacksService.startStack(id),
    onMutate: addBusy,
    onSettled: (_, __, id) => removeBusy(id),
    onSuccess: () => {
      showToast("Stack gestartet", "success");
      invalidate();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Starten"), "error"),
  });

  const stopMutation = useMutation({
    mutationFn: (id: string) => StacksService.stopStack(id),
    onMutate: addBusy,
    onSettled: (_, __, id) => removeBusy(id),
    onSuccess: () => {
      showToast("Stack gestoppt", "success");
      invalidate();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Stoppen"), "error"),
  });

  const restartMutation = useMutation({
    mutationFn: (id: string) => StacksService.restartStack(id),
    onMutate: addBusy,
    onSettled: (_, __, id) => removeBusy(id),
    onSuccess: () => {
      showToast("Stack neu gestartet", "success");
      invalidate();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Neustart"), "error"),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => StacksService.deleteStack(id),
    onMutate: addBusy,
    onSettled: (_, __, id) => removeBusy(id),
    onSuccess: () => {
      showToast("Stack gelöscht", "success");
      invalidate();
      navigate(createRoute("/stacks"));
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Löschen"), "error"),
  });

  const deleteVolumesMutation = useMutation({
    mutationFn: (id: string) => StacksService.deleteStackVolumes(id),
    onSuccess: () => {
      showToast("Volumes gelöscht", "success");
      invalidate();
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Löschen der Volumes"), "error"),
  });

  const cloneMutation = useMutation({
    mutationFn: ({
      projectName,
      newStackName,
      networkName,
    }: {
      projectName: string;
      newStackName?: string;
      networkName?: string;
    }) => StacksService.cloneStack(projectName, { newStackName, networkName }),
    onSuccess: (data) => {
      showToast("Stack geklont", "success");
      invalidate();
      if (data.dockerProjectName)
        navigate(createRoute(`/stacks/${data.dockerProjectName}`), {
          state: { openConfig: true },
        });
    },
    onError: (err: unknown) =>
      showToast(getApiErrorMessage(err, "Fehler beim Klonen"), "error"),
  });

  const isBusyStack = (id: string) => busyIds.has(id);
  const isBusy = busyIds.size > 0;

  return {
    startMutation,
    stopMutation,
    restartMutation,
    deleteMutation,
    deleteVolumesMutation,
    cloneMutation,
    isBusy,
    isBusyStack,
  };
}
