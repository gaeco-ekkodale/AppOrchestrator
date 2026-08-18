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
import { Link } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  MenuItem,
  TextField,
  Typography,
} from "@mui/material";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import { EnvEditor } from "./EnvEditor";
import type { EnvEntry } from "./EnvEditor";
import { createRoute } from "@/utils/routing";
import { mergeNetworkSharedVariables } from "@/features/networks/sharedVariables";

interface DeployRegistryFormProps {
  networks: any[];
  registries: any[];
  noEnvironments: boolean;
  initialPackageId?: string;
  initialVersion?: string;
  isPending: boolean;
  isError: boolean;
  onSubmit: (data: {
    stackName: string;
    registryId: string;
    packageId: string;
    version: string;
    envConfig: Record<string, string> | undefined;
    networkName: string;
  }) => void;
}

export function DeployRegistryForm({
  networks,
  registries,
  noEnvironments,
  initialPackageId = "",
  initialVersion = "",
  isPending,
  isError,
  onSubmit,
}: DeployRegistryFormProps) {
  const [stackName, setStackName] = useState("");
  const [registryId, setRegistryId] = useState("");
  const [packageId, setPackageId] = useState(initialPackageId);
  const [version, setVersion] = useState(initialVersion);
  const [envEntries, setEnvEntries] = useState<EnvEntry[]>([]);
  const [networkName, setNetworkName] = useState("");

  const isRegistryValid =
    stackName.trim() && registryId && packageId.trim() && networkName;

  const handleSubmit = () => {
    const explicitEnv =
      envEntries.length > 0
        ? Object.fromEntries(
            envEntries
              .filter(({ key }) => key.trim())
              .map(({ key, value }) => [key.trim(), value]),
          )
        : {};

    onSubmit({
      stackName,
      registryId,
      packageId,
      version,
      envConfig: mergeNetworkSharedVariables(
        networkName,
        explicitEnv,
        networks,
      ),
      networkName,
    });
  };

  return (
    <>
      <Typography variant="h6" fontWeight="bold" gutterBottom>
        Stack-Konfiguration
      </Typography>
      <Divider sx={{ mb: 3 }} />

      {noEnvironments && (
        <Alert
          severity="warning"
          sx={{ mb: 3 }}
          action={
            <Button
              component={Link}
              to={createRoute("/environments")}
              size="small"
              color="inherit"
            >
              Erstellen
            </Button>
          }
        >
          Zuerst ein Environment erstellen, bevor ein Stack deployed werden
          kann.
        </Alert>
      )}

      <Box sx={{ display: "flex", flexDirection: "column", gap: 2.5 }}>
        <TextField
          label="Stack-Name"
          value={stackName}
          onChange={(e) => setStackName(e.target.value)}
          fullWidth
          required
          helperText="Eindeutiger Name für diesen Stack"
        />
        <TextField
          select
          label="Environment"
          value={networkName}
          onChange={(e) => setNetworkName(e.target.value)}
          fullWidth
          required
          disabled={noEnvironments}
          helperText="Netzwerk, dem dieser Stack beitreten soll"
        >
          {networks.map((n) => (
            <MenuItem key={n.name} value={n.name ?? ""}>
              {n.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Registry"
          value={registryId}
          onChange={(e) => setRegistryId(e.target.value)}
          fullWidth
          required
          helperText="Registry, aus der das Package bezogen wird"
        >
          {registries.length === 0 && (
            <MenuItem disabled>Keine Registries verfügbar</MenuItem>
          )}
          {registries.map((reg) => (
            <MenuItem key={reg.id} value={reg.id}>
              {reg.name} ({reg.baseUrl})
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="Package-ID"
          value={packageId}
          onChange={(e) => setPackageId(e.target.value)}
          fullWidth
          required
          helperText="ID des zu deployenden Packages in der Registry"
        />
        <TextField
          label="Version"
          value={version}
          onChange={(e) => setVersion(e.target.value)}
          fullWidth
          helperText="Optional – leer lassen für die neueste Version"
        />
      </Box>

      <Divider sx={{ my: 3 }} />
      <EnvEditor
        entries={envEntries}
        onChange={setEnvEntries}
        emptyLabel="Optional – Klicke auf + um Variablen hinzuzufügen."
      />

      {isError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          Fehler beim Deployen. Bitte Eingaben prüfen.
        </Alert>
      )}

      <Box
        sx={{
          mt: 3,
          display: "flex",
          justifyContent: "flex-end",
          gap: 2,
        }}
      >
        <Button component={Link} to={createRoute("/stacks")} variant="outlined">
          Abbrechen
        </Button>
        <Button
          variant="contained"
          startIcon={
            isPending ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <CloudUploadIcon />
            )
          }
          onClick={handleSubmit}
          disabled={!isRegistryValid || isPending}
        >
          Deployen
        </Button>
      </Box>
    </>
  );
}
