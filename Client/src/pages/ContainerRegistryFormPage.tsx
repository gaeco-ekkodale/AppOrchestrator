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
  CircularProgress,
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
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import DnsIcon from "@mui/icons-material/Dns";
import VisibilityIcon from "@mui/icons-material/Visibility";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import {useContainerRegistries} from "@/features/dockerRegistries/hooks/useContainerRegistries";
import {
  useCreateContainerRegistryMutation,
  useTestContainerRegistryMutation,
  useUpdateContainerRegistryMutation,
} from "@/features/dockerRegistries/hooks/useContainerRegistryMutations";
import {LoadingSpinner} from "@/features/shared/components";
import {createRoute} from "@/utils/routing";

/**
 * ContainerRegistryFormPage handles both creating and editing a container registry.
 * - /container-registries/new → create mode
 * - /container-registries/:id/edit → edit mode
 *
 * Credentials are never stored — they are forwarded to `docker login` on the host.
 * In edit mode username/password must always be re-entered.
 */
function ContainerRegistryFormPage() {
  const {id} = useParams<{id: string}>();
  const isEditMode = !!id;

  const {containerRegistries, isLoading} = useContainerRegistries();
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [serverAddress, setServerAddress] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [initDone, setInitDone] = useState(false);

  const [testResult, setTestResult] = useState<{
    success: boolean;
    message: string;
  } | null>(null);

  useEffect(() => {
    if (isEditMode && !initDone && containerRegistries.length > 0) {
      const reg = containerRegistries.find((r) => r.id === id);
      if (reg) {
        setName(reg.name ?? "");
        setServerAddress(reg.serverAddress ?? "");
        setInitDone(true);
      }
    }
    if (!isEditMode && !initDone) setInitDone(true);
  }, [containerRegistries, id, isEditMode, initDone]);

  const createMutation = useCreateContainerRegistryMutation(() => {
    navigate(createRoute("/container-registries"));
  });

  const updateMutation = useUpdateContainerRegistryMutation(id!, () => {
    navigate(createRoute("/container-registries"));
  });

  const testMutation = useTestContainerRegistryMutation(
    (data) => {
      setTestResult({success: data.success!, message: data.message!});
    },
    () => {
      setTestResult({
        success: false,
        message: "Verbindungstest fehlgeschlagen.",
      });
    },
  );

  const handleSubmit = () => {
    setTestResult(null);
    if (isEditMode) updateMutation.mutate({name, serverAddress, username, password});
    else createMutation.mutate({name, serverAddress, username, password});
  };

  const isPending = createMutation.isPending || updateMutation.isPending;
  const isValid = name.trim() && serverAddress.trim() && username.trim() && password.trim();
  const canTest = serverAddress.trim() && username.trim() && password.trim();

  if (isLoading && isEditMode && !initDone) return <LoadingSpinner />;

  return (
    <Container maxWidth="sm" sx={{py: 4}}>
      <Button
        component={Link}
        to={createRoute("/container-registries")}
        startIcon={<ArrowBackIcon />}
        sx={{mb: 2}}
      >
        Zurück zur Übersicht
      </Button>

      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
          <DnsIcon color="primary" />
          <Box>
            <Typography variant="h5" fontWeight="bold">
              {isEditMode ? "Container-Registry bearbeiten" : "Container-Registry hinzufügen"}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {isEditMode
                ? "Credentials erneut eingeben, um docker login zu erneuern"
                : "Credentials werden nicht gespeichert — nur docker login wird ausgeführt"}
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
              helperText={"Anzeigename, z.B. \u201eAzure ACR Produktion\u201c"}
            />

            <TextField
              label="Server-Adresse"
              value={serverAddress}
              onChange={(e) => {
                setServerAddress(e.target.value);
                setTestResult(null);
              }}
              fullWidth
              required
              placeholder="myregistry.azurecr.io"
              helperText="Hostname der Registry ohne https://"
            />

            <TextField
              label="Benutzername / Client-ID"
              value={username}
              onChange={(e) => {
                setUsername(e.target.value);
                setTestResult(null);
              }}
              fullWidth
              required
              helperText={
                isEditMode
                  ? "Erneut eingeben (wird nicht gespeichert)"
                  : "z.B. Service-Principal App-ID"
              }
            />

            <TextField
              label="Passwort / PAT / Secret"
              value={password}
              type={showPassword ? "text" : "password"}
              onChange={(e) => {
                setPassword(e.target.value);
                setTestResult(null);
              }}
              fullWidth
              required
              helperText={
                isEditMode
                  ? "Erneut eingeben (wird nicht gespeichert)"
                  : "Passwort, Personal Access Token oder Client-Secret"
              }
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        size="small"
                        onClick={() => setShowPassword((s) => !s)}
                        edge="end"
                        aria-label={showPassword ? "Passwort ausblenden" : "Passwort anzeigen"}
                      >
                        {showPassword ? (
                          <VisibilityOffIcon fontSize="small" />
                        ) : (
                          <VisibilityIcon fontSize="small" />
                        )}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
          </Box>

          {testResult && (
            <Alert
              severity={testResult.success ? "success" : "error"}
              icon={testResult.success ? <CheckCircleIcon /> : undefined}
              sx={{mt: 2}}
            >
              {testResult.message}
            </Alert>
          )}

          <Box
            sx={{
              mt: 3,
              display: "flex",
              justifyContent: "space-between",
              gap: 2,
              flexWrap: "wrap",
            }}
          >
            <Button
              variant="outlined"
              color={
                testResult?.success
                  ? "success"
                  : testResult?.success === false
                    ? "error"
                    : "inherit"
              }
              onClick={() => testMutation.mutate({serverAddress, username, password})}
              disabled={!canTest || testMutation.isPending}
              startIcon={
                testMutation.isPending ? <CircularProgress size={16} color="inherit" /> : undefined
              }
            >
              Verbindung testen
            </Button>

            <Box sx={{display: "flex", gap: 2}}>
              <Button component={Link} to={createRoute("/container-registries")} variant="outlined">
                Abbrechen
              </Button>
              <Button
                variant="contained"
                startIcon={
                  isPending ? <CircularProgress size={16} color="inherit" /> : <SaveIcon />
                }
                onClick={handleSubmit}
                disabled={!isValid || isPending}
              >
                {isEditMode ? "Speichern" : "Hinzufügen"}
              </Button>
            </Box>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
}

export default ContainerRegistryFormPage;
