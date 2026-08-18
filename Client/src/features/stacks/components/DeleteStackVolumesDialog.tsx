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
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from "@mui/material";

interface Props {
  open: boolean;
  stackName?: string;
  isRunning?: boolean;
  loading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
}

export function DeleteStackVolumesDialog({
  open,
  stackName,
  isRunning,
  loading,
  onConfirm,
  onClose,
}: Props) {
  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Volumes löschen</DialogTitle>
      <DialogContent>
        <DialogContentText>
          Möchtest du alle Volumes des Stacks <strong>{stackName}</strong> wirklich
          löschen? Die gespeicherten Daten gehen unwiderruflich verloren.
        </DialogContentText>
        {isRunning && (
          <DialogContentText sx={{mt: 1, color: "warning.main"}}>
            Der Stack läuft noch und wird vor dem Löschen der Volumes automatisch
            gestoppt.
          </DialogContentText>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Abbrechen
        </Button>
        <Button
          onClick={onConfirm}
          color="error"
          variant="contained"
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          Volumes löschen
        </Button>
      </DialogActions>
    </Dialog>
  );
}
