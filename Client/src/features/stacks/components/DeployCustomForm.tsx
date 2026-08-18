// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useState} from "react";
import {Link} from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  FormHelperText,
  MenuItem,
  TextField,
  Typography,
  useTheme,
} from "@mui/material";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import Editor from "@monaco-editor/react";
import {EnvEditor} from "./EnvEditor";
import type {EnvEntry} from "./EnvEditor";
import {createRoute} from "@/utils/routing";
import {mergeNetworkSharedVariables} from "@/features/networks/sharedVariables";

interface DeployCustomFormProps {
  networks: any[];
  noEnvironments: boolean;
  isPending: boolean;
  isError: boolean;
  onSubmit: (data: {
    stackName: string;
    composeContent: string;
    envConfig: Record<string, string> | undefined;
    networkName: string;
  }) => void;
}

export function DeployCustomForm({
  networks,
  noEnvironments,
  isPending,
  isError,
  onSubmit,
}: DeployCustomFormProps) {
  const theme = useTheme();
  const [customStackName, setCustomStackName] = useState("");
  const [composeContent, setComposeContent] = useState("");
  const [customEnvEntries, setCustomEnvEntries] = useState<EnvEntry[]>([]);
  const [customNetworkName, setCustomNetworkName] = useState("");

  const isCustomValid = customStackName.trim() && composeContent.trim() && customNetworkName;

  const handleSubmit = () => {
    const explicitEnv =
      customEnvEntries.length > 0
        ? Object.fromEntries(
            customEnvEntries
              .filter(({key}) => key.trim())
              .map(({key, value}) => [key.trim(), value]),
          )
        : {};

    onSubmit({
      stackName: customStackName,
      composeContent,
      envConfig: mergeNetworkSharedVariables(customNetworkName, explicitEnv, networks),
      networkName: customNetworkName,
    });
  };

  return (
    <>
      <Typography variant="h6" fontWeight="bold" gutterBottom>
        Custom Stack
      </Typography>
      <Divider sx={{mb: 3}} />

      {noEnvironments && (
        <Alert
          severity="warning"
          sx={{mb: 3}}
          action={
            <Button component={Link} to={createRoute("/environments")} size="small" color="inherit">
              Erstellen
            </Button>
          }
        >
          Zuerst ein Environment erstellen, bevor ein Stack deployed werden kann.
        </Alert>
      )}

      <Box sx={{display: "flex", flexDirection: "column", gap: 2.5}}>
        <TextField
          label="Stack-Name"
          value={customStackName}
          onChange={(e) => setCustomStackName(e.target.value)}
          fullWidth
          required
          helperText="Eindeutiger Name für diesen Stack"
        />
        <TextField
          select
          label="Environment"
          value={customNetworkName}
          onChange={(e) => setCustomNetworkName(e.target.value)}
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

        <Alert severity="info">
          <Typography variant="subtitle2" sx={{mb: 1}}>
            Verfügbare Variablen
          </Typography>
          <Box
            component="table"
            sx={{
              width: "100%",
              borderCollapse: "collapse",
              fontFamily: "monospace",
              fontSize: "0.8rem",
              "& td": {py: 0.3, verticalAlign: "top"},
              "& td:first-of-type": {pr: 2, whiteSpace: "nowrap", fontWeight: "bold"},
              "& td:last-of-type": {
                color: "text.secondary",
                fontFamily: "inherit",
                fontSize: "0.8rem",
              },
            }}
          >
            <tbody>
              <tr>
                <td>STACK_NAME</td>
                <td>
                  Eindeutiger Projektname – setzt sich aus Stack-Name und Environment zusammen
                </td>
              </tr>
              <tr>
                <td>ENVIRONMENT_NETWORK</td>
                <td>Netzwerkname der gewählten Environment</td>
              </tr>
              <tr>
                <td>TRAEFIK_NETWORK</td>
                <td>Globales Traefik-Netzwerk für Reverse-Proxy-Anbindung</td>
              </tr>
              <tr>
                <td>VOLUME_BASE_PATH</td>
                <td>
                  Basispfad für Host-Volumes – z.&nbsp;B.{" "}
                  <code>{"${VOLUME_BASE_PATH}/data:/data"}</code>
                </td>
              </tr>
            </tbody>
          </Box>
        </Alert>

        <Box>
          <Typography variant="caption" color={!composeContent.trim() ? "error" : "text.secondary"}>
            docker-compose.yml *
          </Typography>
          <Box
            sx={{
              mt: 0.5,
              border: 1,
              borderColor: !composeContent.trim() ? "error.main" : "divider",
              borderRadius: 1,
              overflow: "hidden",
              "&:focus-within": {
                borderColor: "primary.main",
                boxShadow: (t) => `0 0 0 2px ${t.palette.primary.main}33`,
              },
            }}
          >
            <Editor
              height="400px"
              language="yaml"
              theme={theme.palette.mode === "dark" ? "vs-dark" : "light"}
              value={composeContent}
              onChange={(v) => setComposeContent(v ?? "")}
              options={{
                minimap: {enabled: false},
                fontSize: 13,
                lineNumbers: "on",
                scrollBeyondLastLine: false,
                wordWrap: "off",
                tabSize: 2,
                insertSpaces: true,
                automaticLayout: true,
                padding: {top: 8, bottom: 8},
              }}
            />
          </Box>
          <FormHelperText>Füge hier den Inhalt deiner docker-compose.yml ein</FormHelperText>
        </Box>
      </Box>

      <Divider sx={{my: 3}} />
      <EnvEditor
        entries={customEnvEntries}
        onChange={setCustomEnvEntries}
        emptyLabel="Optional – Klicke auf + um .env-Variablen hinzuzufügen."
      />

      {isError && (
        <Alert severity="error" sx={{mt: 2}}>
          Fehler beim Deployen. Bitte Compose-Inhalt prüfen.
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
            isPending ? <CircularProgress size={16} color="inherit" /> : <CloudUploadIcon />
          }
          onClick={handleSubmit}
          disabled={!isCustomValid || isPending}
        >
          Deployen
        </Button>
      </Box>
    </>
  );
}
