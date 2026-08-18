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
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from "@mui/material";

interface Props {
  open: boolean;
  currentVersion?: string | null;
  targetVersion: string;
  loading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
}

export function UpdateStackDialog({
  open,
  currentVersion,
  targetVersion,
  loading,
  onConfirm,
  onClose,
}: Props) {
  // A version is considered a pre-release when it contains a hyphen (e.g. "1.0.0-test").
  const isPreRelease = targetVersion.includes("-");

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Update bestätigen</DialogTitle>
      <DialogContent>
        <DialogContentText component="div">
          {isPreRelease && (
            <Chip
              label="Versionierter Kanal"
              color="info"
              size="small"
              sx={{ mb: 1.5, display: "flex", width: "fit-content" }}
            />
          )}
          Stack wird von <strong>{currentVersion ?? "-"}</strong> auf{" "}
          <strong>{targetVersion}</strong> aktualisiert.
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Abbrechen
        </Button>
        <Button
          onClick={onConfirm}
          color="primary"
          variant="contained"
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          Jetzt updaten
        </Button>
      </DialogActions>
    </Dialog>
  );
}
