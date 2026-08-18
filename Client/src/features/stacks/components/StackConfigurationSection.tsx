// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import SaveIcon from "@mui/icons-material/Save";
import TuneIcon from "@mui/icons-material/Tune";
import { EnvSchemaForm } from "@/features/store/components";
import { EnvEditor, type EnvEntry } from "./EnvEditor";
import { StackDetailsDTO, StackSource } from "@/api/orchestrator";
import { useRegistries } from "@/features/appRegistries/hooks/useRegistries";
import { useNetworks } from "@/features/networks/hooks/useNetworks";
import {
  getSharedVariablesForNetwork,
  mergeNetworkSharedVariables,
} from "@/features/networks/sharedVariables";
import { useAppVersions } from "@/features/registryClient/hooks/useAppVersions";
import {
  useStackCompose,
  useUpdateStackComposeMutation,
  useUpdateStackMutation,
} from "@/features/stacks/hooks/useStackMutations";
import { createRoute } from "@/utils/routing";

interface StackConfigurationSectionProps {
  stack: StackDetailsDTO;
  configOpen: boolean;
  onConfigOpenChange: (open: boolean) => void;
}

export function StackConfigurationSection({
  stack,
  configOpen,
  onConfigOpenChange,
}: StackConfigurationSectionProps) {
  const navigate = useNavigate();
  const { registries } = useRegistries();
  const { networks } = useNetworks();

  const id = stack.dockerProjectName!;
  const isExternalStack = stack.source === StackSource.EXTERNAL;
  const isRegistryStack = stack.source === StackSource.APP_STORE;
  const isCustomComposeStack = stack.source === StackSource.CUSTOM_COMPOSE;

  const registry = registries.find((r) => r.id === stack.appRegistryId);
  const registryId = registry?.id ?? undefined;
  const packageId = stack.packageId ?? undefined;
  const hasRegistry = !!(registryId && packageId);

  const [version, setVersion] = useState("");
  const [networkName, setNetworkName] = useState("");
  const [envValues, setEnvValues] = useState<Record<string, string>>({});
  const [envEntries, setEnvEntries] = useState<EnvEntry[]>([]);
  const [composeContent, setComposeContent] = useState("");

  const returnToOverviewAfterUpdateRef = useRef(false);

  useEffect(() => {
    if (stack) {
      setVersion(stack.packageVersion ?? "");
      setNetworkName(stack.networkName ?? "");
      setEnvValues(stack.envConfig ?? {});
      setEnvEntries(
        Object.entries(stack.envConfig ?? {}).map(([key, value]) => ({
          key,
          value: value as string,
        })),
      );
    }
  }, [stack?.dockerProjectName]); // eslint-disable-line react-hooks/exhaustive-deps

  const { versions, isLoading: versionsLoading } = useAppVersions(
    registryId ?? "",
    packageId ?? "",
  );

  const { data: composeData } = useStackCompose(
    id,
    !!id && !!stack && isCustomComposeStack,
  );

  useEffect(() => {
    if (composeData?.composeContent !== undefined) {
      setComposeContent(composeData.composeContent ?? "");
    }
  }, [composeData]); // eslint-disable-line react-hooks/exhaustive-deps

  const selectedNetwork = networks.find((n) => n.name === networkName);
  const sharedVariables = getSharedVariablesForNetwork(selectedNetwork);

  const useSchemaForm = !!(isRegistryStack && hasRegistry && version.trim());

  const updateStackMutation = useUpdateStackMutation(id, (updatedStack) => {
    if (returnToOverviewAfterUpdateRef.current) {
      returnToOverviewAfterUpdateRef.current = false;
      navigate(createRoute("/stacks"), { replace: true });
      return;
    }

    const updatedProjectName = updatedStack.dockerProjectName;
    if (!updatedProjectName || updatedProjectName === id) {
      returnToOverviewAfterUpdateRef.current = false;
      return;
    }

    returnToOverviewAfterUpdateRef.current = false;
    navigate(createRoute(`/stacks/${updatedProjectName}`), { replace: true });
  });

  const updateComposeMutation = useUpdateStackComposeMutation(id);

  const isSaving =
    updateStackMutation.isPending || updateComposeMutation.isPending;

  const handleSave = () => {
    if (isExternalStack) return;

    const envConfig: Record<string, string> = {};
    const source = useSchemaForm
      ? Object.entries(envValues)
      : envEntries.map(({ key, value }) => [key, value] as [string, string]);
    source.forEach(([key, value]) => {
      if (key.trim()) envConfig[key.trim()] = value;
    });

    const mergedEnvConfig = mergeNetworkSharedVariables(
      networkName,
      envConfig,
      networks,
    );

    if (isRegistryStack) {
      returnToOverviewAfterUpdateRef.current =
        (networkName || "") !== (stack.networkName || "");

      updateStackMutation.mutate({
        version: version || null,
        envConfig: mergedEnvConfig ?? null,
        networkName: networkName || null,
      });
      return;
    }

    updateComposeMutation.mutate({
      composeContent: composeContent,
      envConfig: mergedEnvConfig ?? {},
      networkName: networkName || null,
    });
  };

  if (isExternalStack) {
    return (
      <Box
        sx={{
          p: 3,
          mb: 2,
          borderRadius: 2,
          border: 1,
          borderColor: "divider",
          bgcolor: "background.paper",
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1 }}>
          <TuneIcon color="action" fontSize="small" />
          <Typography fontWeight="bold">Konfiguration</Typography>
        </Box>
        <Alert severity="info">
          Für externe Stacks ist die Konfiguration in AppOrchestrator
          schreibgeschützt.
        </Alert>
      </Box>
    );
  }

  return (
    <Accordion
      expanded={configOpen}
      onChange={(_, open) => onConfigOpenChange(open)}
      sx={{
        borderRadius: 2,
        "&:before": { display: "none" },
        boxShadow: 1,
        mb: 2,
      }}
    >
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          <TuneIcon color="action" fontSize="small" />
          <Typography fontWeight="bold">Konfiguration bearbeiten</Typography>
        </Box>
      </AccordionSummary>

      <AccordionDetails>
        {isRegistryStack && hasRegistry && (
          <>
            {versionsLoading ? (
              <Box
                sx={{ display: "flex", alignItems: "center", gap: 1, mb: 3 }}
              >
                <CircularProgress size={16} />
                <Typography variant="body2" color="text.secondary">
                  Versionen werden geladen …
                </Typography>
              </Box>
            ) : (
              <FormControl fullWidth size="small" sx={{ mb: 3 }}>
                <InputLabel>Version auswählen</InputLabel>
                <Select
                  value={version}
                  onChange={(e) => setVersion(e.target.value)}
                  label="Version auswählen"
                >
                  {versions.map((v) => (
                    <MenuItem key={v.version} value={v.version!}>
                      <Box
                        sx={{ display: "flex", alignItems: "center", gap: 1 }}
                      >
                        <span>{v.version}</span>
                        {v.isDeprecated && (
                          <Chip
                            label="deprecated"
                            size="small"
                            color="warning"
                            sx={{ height: 18, fontSize: "0.65rem" }}
                          />
                        )}
                        {v.isPreRelease && (
                          <Chip
                            label="pre-release"
                            size="small"
                            color="info"
                            sx={{ height: 18, fontSize: "0.65rem" }}
                          />
                        )}
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}

            <TextField
              select
              fullWidth
              size="small"
              label="Environment"
              value={networkName}
              onChange={(e) => setNetworkName(e.target.value)}
              sx={{ mb: 3 }}
              helperText="Netzwerk, dem dieser Stack beitreten soll"
            >
              {networks.map((n) => (
                <MenuItem key={n.name} value={n.name ?? ""}>
                  {n.name}
                </MenuItem>
              ))}
            </TextField>
            <Divider sx={{ mb: 2 }} />
          </>
        )}

        {isCustomComposeStack && (
          <>
            <Typography variant="subtitle2" gutterBottom>
              Compose YAML
            </Typography>
            <TextField
              multiline
              minRows={8}
              maxRows={24}
              fullWidth
              size="small"
              value={composeContent}
              onChange={(e) => setComposeContent(e.target.value)}
              placeholder="version: '3.8'&#10;services:&#10;  app:&#10;    image: ..."
              inputProps={{
                style: { fontFamily: "monospace", fontSize: "0.8rem" },
              }}
              sx={{ mb: 3 }}
            />
            <Divider sx={{ mb: 2 }} />
          </>
        )}

        {useSchemaForm ? (
          <EnvSchemaForm
            registryId={registryId!}
            packageId={packageId!}
            version={version}
            values={envValues}
            onChange={setEnvValues}
            sharedVariables={sharedVariables}
            networkName={networkName}
          />
        ) : (
          <EnvEditor entries={envEntries} onChange={setEnvEntries} />
        )}

        <Box sx={{ mt: 3, display: "flex", justifyContent: "flex-end" }}>
          <Tooltip title={isSaving ? "Speichern …" : "Änderungen speichern"}>
            <span>
              <Button
                variant="contained"
                startIcon={
                  isSaving ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <SaveIcon />
                  )
                }
                onClick={handleSave}
                disabled={isSaving}
              >
                Speichern & deployen
              </Button>
            </span>
          </Tooltip>
        </Box>
      </AccordionDetails>
    </Accordion>
  );
}
