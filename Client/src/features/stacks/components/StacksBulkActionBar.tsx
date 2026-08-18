// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useState, type ReactNode} from "react";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  Paper,
  Tooltip,
  Typography,
} from "@mui/material";
import {alpha} from "@mui/material/styles";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import StopIcon from "@mui/icons-material/Stop";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import DeleteIcon from "@mui/icons-material/Delete";
import StorageIcon from "@mui/icons-material/Storage";
import CloseIcon from "@mui/icons-material/Close";
import type {StackDTO} from "@/api/orchestrator";
import {
  bulkActionRunningLabel,
  useBulkStackActions,
  type BulkStackAction,
} from "../hooks/useBulkStackActions";
import type {StackSelection} from "../hooks/useStackSelection";
import {canDeleteVolumes, canMutate, canRestart, canStart, canStop, isRunning} from "../stackStatus";

interface ActionDef {
  action: BulkStackAction;
  label: string;
  icon: ReactNode;
  color: "success" | "warning" | "info" | "error";
  /** Whether the action can run on this stack in its current status. */
  applies: (stack: StackDTO) => boolean;
  /** Reason shown when none of the selected stacks accepts the action. */
  unavailableHint: string;
  dialogTitle: string;
  /** Question in the confirmation dialog; count is the number of affected stacks. */
  dialogQuestion: (count: string) => string;
  /** Extra warning line, shown in red-ish for the destructive actions. */
  dialogWarning?: string;
  /** Warning shown only when running stacks are part of the selection. */
  runningWarning?: string;
}

/** "1 Stack" / "3 Stacks" */
function stackCount(n: number): string {
  return n === 1 ? "1 Stack" : `${n} Stacks`;
}

const ACTION_DEFS: ActionDef[] = [
  {
    action: "start",
    label: "Starten",
    icon: <PlayArrowIcon />,
    color: "success",
    applies: (s) => canStart(s.status),
    unavailableHint: "Alle ausgewählten Stacks laufen bereits",
    dialogTitle: "Mehrere Stacks starten",
    dialogQuestion: (count) => `Sollen ${count} gestartet werden?`,
  },
  {
    action: "stop",
    label: "Stoppen",
    icon: <StopIcon />,
    color: "warning",
    applies: (s) => canStop(s.status),
    unavailableHint: "Alle ausgewählten Stacks sind bereits gestoppt",
    dialogTitle: "Mehrere Stacks stoppen",
    dialogQuestion: (count) => `Sollen ${count} gestoppt werden?`,
    dialogWarning: "Die Container sind danach nicht mehr erreichbar.",
  },
  {
    action: "restart",
    label: "Neu starten",
    icon: <RestartAltIcon />,
    color: "info",
    applies: (s) => canRestart(s.status),
    unavailableHint: "Keiner der ausgewählten Stacks läuft",
    dialogTitle: "Mehrere Stacks neu starten",
    dialogQuestion: (count) => `Sollen ${count} neu gestartet werden?`,
    dialogWarning: "Während des Neustarts sind die Container kurz nicht erreichbar.",
  },
  {
    action: "deleteVolumes",
    label: "Volumes löschen",
    icon: <StorageIcon />,
    color: "error",
    applies: canDeleteVolumes,
    unavailableHint: "Für externe Stacks können keine Volumes gelöscht werden",
    dialogTitle: "Volumes mehrerer Stacks löschen",
    dialogQuestion: (count) => `Sollen alle Volumes von ${count} gelöscht werden?`,
    dialogWarning: "Die gespeicherten Daten gehen unwiderruflich verloren.",
    runningWarning: "Laufende Stacks werden vor dem Löschen der Volumes automatisch gestoppt.",
  },
  {
    action: "delete",
    label: "Löschen",
    icon: <DeleteIcon />,
    color: "error",
    applies: (s) => canMutate(s.status),
    unavailableHint: "Die ausgewählten Stacks werden gerade verarbeitet",
    dialogTitle: "Mehrere Stacks löschen",
    dialogQuestion: (count) => `Sollen ${count} gelöscht werden?`,
    dialogWarning:
      "Dies stoppt alle Container und entfernt alle Daten unwiderruflich.",
  },
];

function ConfirmDialog({
  def,
  stacks,
  onConfirm,
  onClose,
}: {
  def?: ActionDef;
  stacks: StackDTO[];
  onConfirm: () => void;
  onClose: () => void;
}) {
  const isDestructive = def?.color === "error";

  return (
    <Dialog open={!!def} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{def?.dialogTitle}</DialogTitle>
      <DialogContent>
        <DialogContentText>
          {def?.dialogQuestion(stackCount(stacks.length))}
        </DialogContentText>

        {/* The affected stacks — this is the subset that accepts the action, not the
            whole selection, so it's worth spelling out. */}
        <Box
          component="ul"
          sx={{mt: 1.5, mb: 0, pl: 3, maxHeight: 220, overflowY: "auto"}}
        >
          {stacks.map((stack) => (
            <Typography
              component="li"
              variant="body2"
              key={stack.dockerProjectName}
              sx={{fontWeight: "medium"}}
            >
              {stack.stackName ?? stack.dockerProjectName}
            </Typography>
          ))}
        </Box>

        {def?.dialogWarning && (
          <DialogContentText
            sx={{mt: 1.5, color: isDestructive ? "error.main" : "text.secondary"}}
          >
            {def.dialogWarning}
          </DialogContentText>
        )}
        {def?.runningWarning && stacks.some((s) => isRunning(s.status)) && (
          <DialogContentText sx={{mt: 1.5, color: "warning.main"}}>
            {def.runningWarning}
          </DialogContentText>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Abbrechen</Button>
        <Button onClick={onConfirm} color={def?.color} variant="contained">
          {def?.label}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

interface StacksBulkActionBarProps {
  /** The stacks currently shown in the table — the selection is resolved against these. */
  stacks: StackDTO[];
  selection: StackSelection;
}

/**
 * Bar shown above the stacks table while at least one stack is selected. Each action only
 * runs on the subset of the selection that actually accepts it (e.g. "Starten" skips stacks
 * that are already running), and the button shows how many stacks that is.
 */
export function StacksBulkActionBar({stacks, selection}: StacksBulkActionBarProps) {
  const [confirmAction, setConfirmAction] = useState<BulkStackAction | null>(null);
  const {run, progress, isRunning: isBulkRunning} = useBulkStackActions();

  const selectedStacks = stacks.filter((s) => selection.isSelected(s.dockerProjectName!));
  if (selectedStacks.length === 0) return null;

  const targetsFor = (def: ActionDef) => selectedStacks.filter(def.applies);
  const confirmDef = ACTION_DEFS.find((d) => d.action === confirmAction);
  const confirmTargets = confirmDef ? targetsFor(confirmDef) : [];

  const execute = (action: BulkStackAction, targets: StackDTO[]) =>
    run(
      action,
      targets.map((s) => s.dockerProjectName!),
    );

  return (
    <>
      <Paper
        sx={{
          px: 2,
          py: 1.5,
          mb: 3,
          borderRadius: 2,
          border: 1,
          borderColor: "primary.main",
          bgcolor: (theme) => alpha(theme.palette.primary.main, 0.06),
          display: "flex",
          alignItems: "center",
          flexWrap: "wrap",
          gap: 1.5,
        }}
      >
        <Chip
          label={`${selectedStacks.length} ausgewählt`}
          color="primary"
          size="small"
          sx={{fontWeight: "bold"}}
        />

        {progress && (
          <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
            <CircularProgress size={16} thickness={5} />
            <Typography variant="body2" color="text.secondary">
              {bulkActionRunningLabel(progress)}
            </Typography>
          </Box>
        )}

        <Box sx={{display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap", ml: "auto"}}>
          {ACTION_DEFS.map((def) => {
            const targets = targetsFor(def);
            const disabled = isBulkRunning || targets.length === 0;

            return (
              <Tooltip
                key={def.action}
                title={
                  targets.length === 0
                    ? def.unavailableHint
                    : `${def.label}: ${targets.length} von ${selectedStacks.length} ausgewählten Stacks`
                }
              >
                <span>
                  <Button
                    variant="outlined"
                    color={def.color}
                    size="small"
                    startIcon={def.icon}
                    disabled={disabled}
                    onClick={() => setConfirmAction(def.action)}
                  >
                    {def.label} ({targets.length})
                  </Button>
                </span>
              </Tooltip>
            );
          })}

          <Tooltip title="Auswahl aufheben">
            <span>
              <IconButton size="small" onClick={selection.clear} disabled={isBulkRunning}>
                <CloseIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      </Paper>

      <ConfirmDialog
        def={confirmDef}
        stacks={confirmTargets}
        onConfirm={() => {
          if (!confirmAction) return;
          const action = confirmAction;
          const targets = confirmTargets;
          // Close first: the list refreshes while the run is in progress, which would make
          // the dialog's own summary shrink stack by stack.
          setConfirmAction(null);
          void execute(action, targets);
        }}
        onClose={() => setConfirmAction(null)}
      />
    </>
  );
}
