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
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { networksQueryOptions } from "@/features/networks/queries";

interface Props {
  open: boolean;
  sourceStackName?: string;
  sourceNetworkName?: string | null;
  loading?: boolean;
  onConfirm: (values: { newName?: string; networkName?: string }) => void;
  onClose: () => void;
}

const KEEP_SOURCE_NETWORK = "";

export function CloneStackDialog({
  open,
  sourceStackName,
  sourceNetworkName,
  loading,
  onConfirm,
  onClose,
}: Props) {
  const [newName, setNewName] = useState("");
  const [networkName, setNetworkName] = useState(KEEP_SOURCE_NETWORK);
  const { data: networks } = useQuery(networksQueryOptions);

  const trimmedName = newName.trim();
  const nameChanged = trimmedName !== "" && trimmedName !== sourceStackName;
  const networkChanged =
    networkName !== KEEP_SOURCE_NETWORK && networkName !== sourceNetworkName;

  // The project name is derived from stack name and network, so at least one must differ.
  const canConfirm = nameChanged || networkChanged;

  const handleConfirm = () => {
    if (!canConfirm) return;
    onConfirm({
      newName: trimmedName || undefined,
      networkName: networkChanged ? networkName : undefined,
    });
  };

  const handleClose = () => {
    setNewName("");
    setNetworkName(KEEP_SOURCE_NETWORK);
    onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="xs" fullWidth>
      <DialogTitle>Stack klonen</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 0.5 }}>
          <Alert severity="info">
            Der Klon übernimmt den kompletten Workspace inklusive Paket-Dateien und
            Volume-Daten. Vergib einen anderen Namen oder wähle ein anderes Netzwerk.
          </Alert>

          <TextField
            autoFocus
            label="Neuer Stack-Name"
            fullWidth
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            helperText={
              trimmedName === "" ? `Leer lassen behält "${sourceStackName}"` : undefined
            }
            onKeyDown={(e) => e.key === "Enter" && handleConfirm()}
          />

          <TextField
            select
            label="Netzwerk"
            fullWidth
            value={networkName}
            onChange={(e) => setNetworkName(e.target.value)}
            helperText={`Aktuell: ${sourceNetworkName || "kein Netzwerk"}`}
          >
            <MenuItem value={KEEP_SOURCE_NETWORK}>
              <em>Netzwerk übernehmen</em>
            </MenuItem>
            {(networks ?? [])
              .filter((network) => !!network.name)
              .map((network) => (
                <MenuItem key={network.name} value={network.name!}>
                  {network.name}
                </MenuItem>
              ))}
          </TextField>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={loading}>
          Abbrechen
        </Button>
        <Button
          onClick={handleConfirm}
          variant="contained"
          disabled={loading || !canConfirm}
        >
          Klonen
        </Button>
      </DialogActions>
    </Dialog>
  );
}
