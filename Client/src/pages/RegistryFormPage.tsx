// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useEffect, useState} from "react";
import {Link, useNavigate, useParams} from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Container,
  Divider,
  IconButton,
  InputAdornment,
  Paper,
  TextField,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import SaveIcon from "@mui/icons-material/Save";
import StorageIcon from "@mui/icons-material/Storage";
import VisibilityIcon from "@mui/icons-material/Visibility";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import {useRegistries} from "@/features/appRegistries/hooks/useRegistries";
import {
  useCreateAppRegistryMutation,
  useUpdateAppRegistryMutation,
} from "@/features/appRegistries/hooks/useAppRegistryMutations";
import {LoadingSpinner} from "@/features/shared/components";
import {createRoute} from "@/utils/routing";

/**
 * RegistryFormPage handles both creating and editing a registry.
 * - /registries/new → create mode
 * - /registries/:id/edit → edit mode
 */
function RegistryFormPage() {
  const {id} = useParams<{id: string}>();
  const isEditMode = !!id;

  const {registries, isLoading} = useRegistries();
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [baseUrl, setBaseUrl] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [showApiKey, setShowApiKey] = useState(false);
  const [initDone, setInitDone] = useState(false);

  useEffect(() => {
    if (isEditMode && !initDone && registries.length > 0) {
      const reg = registries.find((r) => r.id === id);
      if (reg) {
        setName(reg.name ?? "");
        setBaseUrl(reg.baseUrl ?? "");
        setInitDone(true);
      }
    }
    if (!isEditMode && !initDone) setInitDone(true);
  }, [registries, id, isEditMode, initDone]);

  const createMutation = useCreateAppRegistryMutation(() => {
    navigate(createRoute("/registries"));
  });

  const updateMutation = useUpdateAppRegistryMutation(id!, () => {
    navigate(createRoute("/registries"));
  });

  const handleSubmit = () => {
    const trimmedKey = apiKey.trim();
    if (isEditMode) updateMutation.mutate({name, baseUrl, apiKey: trimmedKey || undefined});
    else createMutation.mutate({name, baseUrl, apiKey: trimmedKey});
  };

  const isPending = createMutation.isPending || updateMutation.isPending;
  const isError = createMutation.isError || updateMutation.isError;
  const isValid = name.trim() && baseUrl.trim();
  const existingRegistry = isEditMode ? registries.find((r) => r.id === id) : null;

  if (isLoading && isEditMode && !initDone) return <LoadingSpinner />;

  return (
    <Container maxWidth="sm" sx={{py: 4}}>
      <Button
        component={Link}
        to={createRoute("/registries")}
        startIcon={<ArrowBackIcon />}
        sx={{mb: 2}}
      >
        Zurück zur Übersicht
      </Button>

      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
          <StorageIcon color="primary" />
          <Box>
            <Typography variant="h5" fontWeight="bold">
              {isEditMode ? "Registry bearbeiten" : "Registry hinzufügen"}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {isEditMode ? "Bestehende Registry aktualisieren" : "Neue App-Registry registrieren"}
            </Typography>
          </Box>
        </Box>
      </Paper>

      <Card sx={{borderRadius: 2}}>
        <CardContent sx={{p: 3}}>
          <Typography variant="h6" fontWeight="bold" gutterBottom>
            Registry-Details
          </Typography>
          <Divider sx={{mb: 3}} />

          <Box sx={{display: "flex", flexDirection: "column", gap: 2.5}}>
            <TextField
              label="Name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              fullWidth
              required
              helperText="Anzeigename der Registry"
            />

            <TextField
              label="Base URL"
              value={baseUrl}
              onChange={(e) => setBaseUrl(e.target.value)}
              fullWidth
              required
              helperText="Basis-URL der Registry (z.B. https://registry.example.com)"
              placeholder="https://registry.example.com"
            />

            <TextField
              label={isEditMode ? "API Key (leer lassen = unverändert)" : "API Key"}
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              type={showApiKey ? "text" : "password"}
              fullWidth
              helperText={
                isEditMode && existingRegistry?.hasApiKey
                  ? "Aktuell ist ein API Key gespeichert. Neuen Key eingeben zum Ersetzen."
                  : "API Key zur Authentifizierung bei der Registry."
              }
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      {isEditMode && existingRegistry?.hasApiKey && !apiKey && (
                        <Chip label="Key gesetzt" size="small" color="success" sx={{mr: 1}} />
                      )}
                      <IconButton size="small" onClick={() => setShowApiKey((s) => !s)}>
                        {showApiKey ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
          </Box>

          {isError && (
            <Alert severity="error" sx={{mt: 2}}>
              Fehler beim Speichern. Bitte Eingaben prüfen.
            </Alert>
          )}

          <Box sx={{mt: 3, display: "flex", justifyContent: "flex-end", gap: 2}}>
            <Button component={Link} to={createRoute("/registries")} variant="outlined">
              Abbrechen
            </Button>
            <Button
              variant="contained"
              startIcon={<SaveIcon />}
              onClick={handleSubmit}
              disabled={!isValid || isPending}
            >
              {isEditMode ? "Speichern" : "Hinzufügen"}
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
}

export default RegistryFormPage;
