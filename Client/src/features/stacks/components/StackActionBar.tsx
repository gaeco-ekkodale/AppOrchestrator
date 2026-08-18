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
import { Box, Button, CircularProgress, Tooltip } from "@mui/material";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import StopIcon from "@mui/icons-material/Stop";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import DeleteIcon from "@mui/icons-material/Delete";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import StorageIcon from "@mui/icons-material/Storage";
import { StackDetailsDTO, StackStatus } from "@/api/orchestrator";
import { useStackActions } from "../hooks/useStackActions";
import { canMutate, canRestart, canStart, canStop, isTransitioning } from "../stackStatus";
import { DeleteStackDialog } from "./DeleteStackDialog";
import { DeleteStackVolumesDialog } from "./DeleteStackVolumesDialog";
import { CloneStackDialog } from "./CloneStackDialog";

interface StackActionBarProps {
  stack: StackDetailsDTO;
  isExternal?: boolean;
}

export function StackActionBar({
  stack,
  isExternal = false,
}: StackActionBarProps) {
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteVolumesOpen, setDeleteVolumesOpen] = useState(false);
  const [cloneOpen, setCloneOpen] = useState(false);

  const id = stack.dockerProjectName!;
  const status = stack.status;
  const {
    startMutation,
    stopMutation,
    restartMutation,
    deleteMutation,
    deleteVolumesMutation,
    cloneMutation,
    isBusy,
  } = useStackActions(id);

  const transitioning = isTransitioning(status);
  const isRunning = status === StackStatus.RUNNING;
  const isStopped = status === StackStatus.STOPPED;
  const isPartial = status === StackStatus.PARTIAL;

  const startDisabled = isBusy || !canStart(status);
  const stopDisabled = isBusy || !canStop(status);
  const restartDisabled = isBusy || !canRestart(status);
  const mutateDisabled = isBusy || !canMutate(status);

  const startTooltip =
    isRunning || isPartial
      ? "Stack läuft bereits"
      : transitioning
        ? "Stack wird gerade verarbeitet"
        : "Starten";
  const stopTooltip = isStopped
    ? "Stack ist bereits gestoppt"
    : transitioning
      ? "Stack wird gerade verarbeitet"
      : "Stoppen";
  const restartTooltip = isStopped
    ? "Stack ist gestoppt – erst starten"
    : transitioning
      ? "Stack wird gerade verarbeitet"
      : "Neu starten";

  return (
    <>
      <Box
        sx={{ display: "flex", gap: 1, flexWrap: "wrap", alignItems: "center" }}
      >
        {isBusy && (
          <CircularProgress size={18} thickness={5} sx={{ mr: 0.5 }} />
        )}

        <Tooltip title={startTooltip}>
          <span>
            <Button
              variant={isRunning ? "contained" : "outlined"}
              color="success"
              startIcon={<PlayArrowIcon />}
              onClick={() => startMutation.mutate(id)}
              disabled={startDisabled}
              size="small"
            >
              Starten
            </Button>
          </span>
        </Tooltip>

        <Tooltip title={stopTooltip}>
          <span>
            <Button
              variant="outlined"
              color="warning"
              startIcon={<StopIcon />}
              onClick={() => stopMutation.mutate(id)}
              disabled={stopDisabled}
              size="small"
            >
              Stoppen
            </Button>
          </span>
        </Tooltip>

        <Tooltip title={restartTooltip}>
          <span>
            <Button
              variant="outlined"
              color="info"
              startIcon={<RestartAltIcon />}
              onClick={() => restartMutation.mutate(id)}
              disabled={restartDisabled}
              size="small"
            >
              Neu starten
            </Button>
          </span>
        </Tooltip>

        {!isExternal && (
          <Tooltip title="Klonen">
            <span>
              <Button
                variant="outlined"
                startIcon={<ContentCopyIcon />}
                onClick={() => setCloneOpen(true)}
                disabled={mutateDisabled}
                size="small"
              >
                Klonen
              </Button>
            </span>
          </Tooltip>
        )}

        {!isExternal && (
          <Tooltip title="Volumes löschen">
            <span>
              <Button
                variant="outlined"
                color="error"
                startIcon={<StorageIcon />}
                onClick={() => setDeleteVolumesOpen(true)}
                disabled={mutateDisabled}
                size="small"
              >
                Volumes löschen
              </Button>
            </span>
          </Tooltip>
        )}

        <Tooltip title="Löschen">
          <span>
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteIcon />}
              onClick={() => setDeleteOpen(true)}
              disabled={mutateDisabled}
              size="small"
            >
              Löschen
            </Button>
          </span>
        </Tooltip>
      </Box>

      {/* Dialogs */}
      <DeleteStackVolumesDialog
        open={deleteVolumesOpen}
        stackName={stack.stackName}
        isRunning={isRunning || isPartial}
        loading={deleteVolumesMutation.isPending}
        onConfirm={() =>
          deleteVolumesMutation.mutate(id, {onSuccess: () => setDeleteVolumesOpen(false)})
        }
        onClose={() => setDeleteVolumesOpen(false)}
      />

      <DeleteStackDialog
        open={deleteOpen}
        stackName={stack.stackName}
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate(id)}
        onClose={() => setDeleteOpen(false)}
      />

      <CloneStackDialog
        open={cloneOpen}
        sourceStackName={stack.stackName}
        sourceNetworkName={stack.networkName}
        loading={cloneMutation.isPending}
        onConfirm={({ newName, networkName }) =>
          cloneMutation.mutate({
            projectName: id,
            newStackName: newName,
            networkName,
          })
        }
        onClose={() => setCloneOpen(false)}
      />
    </>
  );
}
