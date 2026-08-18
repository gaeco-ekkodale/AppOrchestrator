// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  Chip,
  Alert,
  Button,
  LinearProgress,
  Paper,
} from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import SyncIcon from "@mui/icons-material/Sync";
import { useNavigate } from "react-router-dom";
import { createRoute } from "@/utils/routing";
import type { DeployStatus } from "../hooks/useProjectDeploy";

export interface ProjectDeployProgressProps {
  statuses: DeployStatus[];
  isDeploying: boolean;
  onClose?: () => void;
}

const STATUS_ICON: Record<DeployStatus["status"], React.ReactNode> = {
  waiting: <HourglassEmptyIcon />,
  deploying: <SyncIcon sx={{ animation: "spin 1s linear infinite" }} />,
  done: <CheckCircleIcon sx={{ color: "success.main" }} />,
  error: <ErrorIcon sx={{ color: "error.main" }} />,
  "rolling-back": <SyncIcon sx={{ animation: "spin 1s linear infinite", color: "warning.main" }} />,
  "rolled-back": <CheckCircleIcon sx={{ color: "warning.main" }} />,
};

const STATUS_COLOR: Record<DeployStatus["status"], "info" | "success" | "error" | "warning"> = {
  waiting: "info",
  deploying: "info",
  done: "success",
  error: "error",
  "rolling-back": "warning",
  "rolled-back": "warning",
};

const STATUS_LABEL: Record<DeployStatus["status"], string> = {
  waiting: "Warten...",
  deploying: "Wird deployt...",
  done: "Erfolgreich",
  error: "Fehler",
  "rolling-back": "Wird zurückgerollt...",
  "rolled-back": "Zurückgerollt",
};

export function ProjectDeployProgress({
  statuses,
  isDeploying,
  onClose,
}: ProjectDeployProgressProps) {
  const navigate = useNavigate();

  const successCount = statuses.filter((s) => s.status === "done").length;
  const errorCount = statuses.filter((s) => s.status === "error").length;
  const progress = statuses.length > 0 ? (successCount / statuses.length) * 100 : 0;

  const hasErrors = errorCount > 0;
  const isComplete = !isDeploying;

  // Group statuses by step
  const stepMap = new Map<number, DeployStatus[]>();
  for (const s of statuses) {
    const list = stepMap.get(s.stepIndex) ?? [];
    list.push(s);
    stepMap.set(s.stepIndex, list);
  }
  const sortedStepIndices = Array.from(stepMap.keys()).sort((a, b) => a - b);

  const handleViewStack = (dockerProjectName: string) => {
    navigate(createRoute(`/stacks/${dockerProjectName}`));
  };

  return (
    <Box>
      <LinearProgress variant="determinate" value={progress} sx={{ mb: 2 }} />

      <Typography variant="h6" sx={{ mb: 2 }}>
        Deployment-Fortschritt ({successCount}/{statuses.length})
      </Typography>

      {hasErrors && isComplete && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Deployment fehlgeschlagen.{" "}
          {statuses.filter((s) => s.status === "rolled-back").length} App(s) wurden zurückgerollt.
        </Alert>
      )}

      {!hasErrors && isComplete && statuses.length > 0 && (
        <Alert severity="success" sx={{ mb: 2 }}>
          Projekt erfolgreich deployt!
        </Alert>
      )}

      {/* Steps grouped */}
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
        {sortedStepIndices.map((stepIndex) => {
          const stepStatuses = stepMap.get(stepIndex)!;
          return (
            <Paper key={stepIndex} variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
              <Box sx={{ px: 2, py: 1, bgcolor: "grey.100", borderBottom: "1px solid", borderColor: "divider" }}>
                <Typography variant="subtitle2" fontWeight="bold">
                  Schritt {stepIndex + 1}
                  {stepStatuses.length > 1 && (
                    <Chip
                      label={`${stepStatuses.length} Apps parallel`}
                      size="small"
                      sx={{ ml: 1, height: 18, fontSize: "0.7rem" }}
                    />
                  )}
                </Typography>
              </Box>

              <List dense disablePadding>
                {stepStatuses.map((status) => (
                  <ListItem
                    key={status.id}
                    sx={{
                      bgcolor:
                        status.status === "error"
                          ? "error.lighter"
                          : status.status === "done"
                            ? "success.lighter"
                            : "transparent",
                      borderRadius: 1,
                      mx: 1,
                      my: 0.5,
                    }}
                    secondaryAction={
                      status.status === "done" && status.dockerProjectName ? (
                        <Button size="small" onClick={() => handleViewStack(status.dockerProjectName!)}>
                          Anzeigen
                        </Button>
                      ) : null
                    }
                  >
                    <Box sx={{ mr: 2, display: "flex", alignItems: "center" }}>
                      {STATUS_ICON[status.status]}
                    </Box>
                    <ListItemText
                      primary={status.stackName}
                      secondary={status.error || STATUS_LABEL[status.status]}
                      secondaryTypographyProps={{
                        color: status.status === "error" ? "error" : "textSecondary",
                      }}
                    />
                    <Chip
                      label={STATUS_LABEL[status.status]}
                      color={STATUS_COLOR[status.status]}
                      size="small"
                      variant="outlined"
                      sx={{ mr: status.status === "done" && status.dockerProjectName ? 8 : 0 }}
                    />
                  </ListItem>
                ))}
              </List>
            </Paper>
          );
        })}
      </Box>

      {isComplete && (
        <Box sx={{ display: "flex", gap: 1, mt: 3, justifyContent: "flex-end" }}>
          {!hasErrors && (
            <Button variant="outlined" onClick={() => navigate(createRoute("/environments"))}>
              Zu Umgebungen
            </Button>
          )}
          {onClose && (
            <Button variant="contained" onClick={onClose}>
              Schließen
            </Button>
          )}
        </Box>
      )}

      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </Box>
  );
}
