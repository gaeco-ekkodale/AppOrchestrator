// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, CircularProgress, Typography, Chip } from "@mui/material";
import MemoryIcon from "@mui/icons-material/Memory";
import { useState } from "react";
import { type ContainerDTO } from "@/api/orchestrator";
import {
  useStartContainerMutation,
  useStopContainerMutation,
} from "@/features/stacks/hooks/useStackContainerActions";
import { useStackContainers } from "../hooks/useStackContainers";
import { ContainerLogsDialog } from "./ContainerLogsDialog";
import { CompactStackContainersTable } from "./CompactStackContainersTable";

interface StackContainersPanelProps {
  stackId: string;
  projectName?: string;
  showPorts?: boolean;
  hideHeader?: boolean;
}

function stateColor(
  state?: string,
): "success" | "error" | "warning" | "default" {
  const s = state?.toLowerCase();
  if (s === "running") return "success";
  if (s === "exited" || s === "dead") return "error";
  if (s === "paused") return "warning";
  return "default";
}

export function StackContainersPanel({
  stackId,
  projectName,
  showPorts = false,
  hideHeader = false,
}: StackContainersPanelProps) {
  const { containers, isLoading } = useStackContainers(stackId);
  const [busyContainerId, setBusyContainerId] = useState<string | null>(null);
  const [logsContainer, setLogsContainer] = useState<ContainerDTO | null>(null);
  const startContainerMutation = useStartContainerMutation();
  const stopContainerMutation = useStopContainerMutation();

  const canManageContainers = !!projectName;

  const doContainerAction = async (
    action: "start" | "stop",
    containerId?: string,
  ) => {
    if (!projectName || !containerId) return;

    try {
      setBusyContainerId(containerId);

      if (action === "start") {
        await startContainerMutation.mutateAsync({ projectName, containerId });
      } else {
        await stopContainerMutation.mutateAsync({ projectName, containerId });
      }
    } catch {
    } finally {
      setBusyContainerId(null);
    }
  };

  const closeLogs = () => {
    setLogsContainer(null);
  };

  if (isLoading) {
    return (
      <>
        {!hideHeader && (
          <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2 }}>
            <MemoryIcon color="action" fontSize="small" />
            <Typography variant="h6" fontWeight="bold">
              Container
            </Typography>
          </Box>
        )}
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, p: 1 }}>
          <CircularProgress size={16} />
          <Typography variant="body2" color="text.secondary">
            Container werden geladen …
          </Typography>
        </Box>
      </>
    );
  }

  if (containers.length === 0) {
    return (
      <>
        {!hideHeader && (
          <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2 }}>
            <MemoryIcon color="action" fontSize="small" />
            <Typography variant="h6" fontWeight="bold">
              Container
            </Typography>
            <Chip label="0" size="small" />
          </Box>
        )}
        <Typography variant="body2" color="text.secondary" sx={{ p: 1 }}>
          Keine Container gefunden.
        </Typography>
      </>
    );
  }

  return (
    <>
      {!hideHeader && (
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2 }}>
          <MemoryIcon color="action" fontSize="small" />
          <Typography variant="h6" fontWeight="bold">
            Container
          </Typography>
          <Chip label={containers.length} size="small" />
        </Box>
      )}

      <CompactStackContainersTable
        containers={containers}
        busyContainerId={busyContainerId}
        canManageContainers={canManageContainers}
        showPorts={showPorts}
        getStateColor={stateColor}
        onStart={(containerId) => void doContainerAction("start", containerId)}
        onStop={(containerId) => void doContainerAction("stop", containerId)}
        onLogs={(container) => setLogsContainer(container)}
      />

      <ContainerLogsDialog
        open={!!logsContainer}
        projectName={projectName}
        container={logsContainer}
        pollIntervalMs={1500}
        onClose={closeLogs}
      />
    </>
  );
}
