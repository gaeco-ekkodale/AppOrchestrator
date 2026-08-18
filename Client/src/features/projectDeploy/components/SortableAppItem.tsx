// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Box, Chip, IconButton, Paper, Tooltip, Typography } from "@mui/material";
import DragIndicatorIcon from "@mui/icons-material/DragIndicator";
import CloseIcon from "@mui/icons-material/Close";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { AppIcon } from "@/features/store/components/AppIcon";
import type { ProjectApp } from "../types";

export interface SortableAppItemProps {
  app: ProjectApp;
  index: number;
  onRemove: () => void;
  alreadyDeployed?: boolean;
}

/** Shared card content — used by both sortable item and DragOverlay */
export function AppItemCard({
  app,
  index,
  onRemove,
  alreadyDeployed,
  dragHandleProps,
  isDragging,
}: {
  app: ProjectApp;
  index: number;
  onRemove?: () => void;
  alreadyDeployed?: boolean;
  dragHandleProps?: React.HTMLAttributes<HTMLElement>;
  isDragging?: boolean;
}) {
  return (
    <Paper
      variant="outlined"
      sx={{
        display: "flex",
        alignItems: "center",
        gap: 1,
        px: 1,
        py: 0.75,
        mb: 0.75,
        borderRadius: 1.5,
        bgcolor: isDragging ? "action.selected" : "background.paper",
        opacity: isDragging ? 0.5 : 1,
        cursor: dragHandleProps ? "default" : undefined,
        userSelect: "none",
      }}
    >
      {/* Drag handle */}
      <Box
        {...dragHandleProps}
        sx={{ cursor: "grab", color: "text.disabled", display: "flex", flexShrink: 0 }}
      >
        <DragIndicatorIcon fontSize="small" />
      </Box>

      {/* Step index */}
      <Typography variant="caption" color="text.disabled" sx={{ minWidth: 16, flexShrink: 0 }}>
        {index}.
      </Typography>

      {/* App icon */}
      <AppIcon name={app.name} iconUrl={app.iconUrl} size={28} />

      {/* App info */}
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
          <Typography variant="body2" fontWeight="bold" noWrap>
            {app.name}
          </Typography>
          {alreadyDeployed && (
            <Tooltip title="Bereits in dieser Umgebung deployt">
              <WarningAmberIcon fontSize="small" sx={{ color: "warning.main", flexShrink: 0 }} />
            </Tooltip>
          )}
        </Box>
        <Typography variant="caption" color="text.secondary" noWrap>
          {app.packageId}
        </Typography>
      </Box>

      {/* Version + registry chips */}
      <Box sx={{ display: "flex", gap: 0.5, flexShrink: 0 }}>
        <Chip label={`v${app.version}`} size="small" variant="outlined" />
        <Chip label={app.registryName} size="small" />
      </Box>

      {/* Remove button */}
      {onRemove && (
        <IconButton size="small" onClick={onRemove} aria-label="entfernen" sx={{ flexShrink: 0 }}>
          <CloseIcon fontSize="small" />
        </IconButton>
      )}
    </Paper>
  );
}

export function SortableAppItem({ app, index, onRemove, alreadyDeployed }: SortableAppItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: app.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <Box ref={setNodeRef} style={style}>
      <AppItemCard
        app={app}
        index={index}
        onRemove={onRemove}
        alreadyDeployed={alreadyDeployed}
        dragHandleProps={{ ...attributes, ...listeners }}
        isDragging={isDragging}
      />
    </Box>
  );
}
