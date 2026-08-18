// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Chip } from "@mui/material";
import CircleIcon from "@mui/icons-material/Circle";
import { StackStatus } from "@/api/orchestrator";

const STATUS_LABELS: Record<StackStatus, string> = {
  [StackStatus.UNKNOWN]: "Unbekannt",
  [StackStatus.INSTALLING]: "Wird installiert",
  [StackStatus.RUNNING]: "Läuft",
  [StackStatus.PARTIAL]: "Teilweise",
  [StackStatus.STOPPED]: "Gestoppt",
  [StackStatus.UPDATING]: "Wird aktualisiert",
  [StackStatus.FAILED]: "Fehler",
};

const STATUS_COLORS: Record<
  StackStatus,
  "default" | "primary" | "success" | "warning" | "error" | "info"
> = {
  [StackStatus.UNKNOWN]: "default",
  [StackStatus.INSTALLING]: "info",
  [StackStatus.RUNNING]: "success",
  [StackStatus.PARTIAL]: "warning",
  [StackStatus.STOPPED]: "default",
  [StackStatus.UPDATING]: "info",
  [StackStatus.FAILED]: "error",
};

/** Status values that indicate the stack is currently transitioning. */
const TRANSITIONING = new Set([StackStatus.INSTALLING, StackStatus.UPDATING]);

interface Props {
  status?: StackStatus;
}

export function StackStatusChip({ status }: Props) {
  const s = status ?? StackStatus.UNKNOWN;
  const label = STATUS_LABELS[s] ?? "Unbekannt";
  const color = STATUS_COLORS[s] ?? "default";
  const isTransitioning = TRANSITIONING.has(s);

  return (
    <Chip
      label={
        isTransitioning ? (
          <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
            <CircleIcon
              sx={{
                fontSize: 8,
                "@keyframes pulse": {
                  "0%, 100%": { opacity: 1 },
                  "50%": { opacity: 0.2 },
                },
                animation: "pulse 1.2s ease-in-out infinite",
              }}
            />
            {label}
          </Box>
        ) : (
          label
        )
      }
      color={color}
      size="small"
      variant={isTransitioning ? "outlined" : "filled"}
    />
  );
}
