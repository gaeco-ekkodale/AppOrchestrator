// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useMemo, useState} from "react";
import {Alert, Box, Button, Chip, Typography, Divider} from "@mui/material";
import {StackDetailsDTO, StackSource} from "@/api/orchestrator";
import {useQuery} from "@tanstack/react-query";
import {AppIcon} from "@/features/store/components";
import {StackStatusChip} from "./StackStatusChip";
import {UpdateStackDialog} from "./UpdateStackDialog";
import {useRegistries} from "@/features/appRegistries/hooks/useRegistries";
import {useNetworks} from "@/features/networks/hooks/useNetworks";
import {useAppVersions} from "@/features/registryClient/hooks/useAppVersions";
import {appDetailQueryOptions} from "@/features/registryClient/queries";
import {useEnvSchema} from "@/features/registryClient/hooks/useEnvSchema";
import {useUpdateStackMutation} from "@/features/stacks/hooks/useStackMutations";

interface StackInfoSectionProps {
  stack: StackDetailsDTO;
}

export function StackInfoSection({stack}: StackInfoSectionProps) {
  const [updateDialogOpen, setUpdateDialogOpen] = useState(false);

  const {registries} = useRegistries();
  const {networks} = useNetworks();
  const isExternalStack = stack.source === StackSource.EXTERNAL;
  const isRegistryStack = stack.source === StackSource.APP_STORE;

  const registry = registries.find((r) => r.id === stack.appRegistryId);
  const registryId = registry?.id ?? undefined;
  const packageId = stack.packageId ?? undefined;
  const hasRegistry = !!(registryId && packageId);

  // Allowed version suffixes from the network this stack belongs to.
  // Empty list = no restriction (all versions shown as updates).
  const network = networks.find((n) => n.name === stack.networkName);
  const allowedSuffixes = network?.allowedVersionSuffixes ?? [];

  const {data: appDetail} = useQuery({
    ...appDetailQueryOptions(registryId ?? "", packageId ?? ""),
    enabled: !!stack && isRegistryStack && hasRegistry,
  });

  const {versions} = useAppVersions(registryId ?? "", packageId ?? "");

  // Filter versions by the network's allowed suffixes.
  // A version is eligible when:
  //   - no suffixes are configured (no restriction), OR
  //   - the version's pre-release part (after the first "-") matches an allowed suffix.
  const latestVersion = useMemo(() => {
    if (!versions.length) return appDetail?.defaultVersion ?? "";
    const eligible =
      allowedSuffixes.length === 0
        ? versions
        : versions.filter((v) => {
            const dash = v.version.indexOf("-");
            const preRelease = dash >= 0 ? v.version.slice(dash + 1).toLowerCase() : "";
            return allowedSuffixes.some((s) =>
              s === "" ? dash < 0 : preRelease === s.toLowerCase(),
            );
          });
    return eligible[0]?.version ?? "";
  }, [versions, allowedSuffixes, appDetail?.defaultVersion]);

  const hasUpdateAvailable =
    isRegistryStack && !!latestVersion && latestVersion !== (stack.packageVersion ?? "");

  const {schema: currentSchema, isLoading: currentSchemaLoading} = useEnvSchema(
    registryId ?? "",
    packageId ?? "",
    stack.packageVersion ?? "",
  );

  const {schema: targetSchema, isLoading: targetSchemaLoading} = useEnvSchema(
    registryId ?? "",
    packageId ?? "",
    hasUpdateAvailable ? latestVersion : "",
  );

  const noNewSchemaFields = useMemo(() => {
    if (!hasUpdateAvailable) return false;
    const currentNames = new Set(currentSchema.map((field) => field.name));
    return targetSchema.every((field) => currentNames.has(field.name));
  }, [hasUpdateAvailable, currentSchema, targetSchema]);

  const canOneClickUpdate =
    hasUpdateAvailable && !currentSchemaLoading && !targetSchemaLoading && noNewSchemaFields;

  const updateStackMutation = useUpdateStackMutation(stack.dockerProjectName!);

  const handleUpdateConfirm = () => {
    if (!stack || !canOneClickUpdate) return;
    updateStackMutation.mutate(
      {version: latestVersion},
      {onSettled: () => setUpdateDialogOpen(false)},
    );
  };

  return (
    <>
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          flexWrap: "wrap",
          gap: 2,
          mb: 2,
        }}
      >
        <Box>
          <Typography variant="h4" fontWeight="bold">
            {stack.stackName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Docker-Projekt: {stack.dockerProjectName}
          </Typography>
        </Box>
        <StackStatusChip status={stack.status} />
      </Box>

      <Divider sx={{mb: 2}} />

      {isRegistryStack && (
        <Box sx={{display: "flex", alignItems: "center", gap: 1.5, mb: 2}}>
          <AppIcon
            name={
              appDetail?.name ??
              stack.packageId ??
              stack.stackName ??
              stack.dockerProjectName ??
              "App"
            }
            iconUrl={appDetail?.iconUrl}
            size={36}
          />
          <Box>
            <Typography variant="body2" fontWeight={600}>
              {appDetail?.name ?? stack.packageId}
            </Typography>
            {appDetail?.description && (
              <Typography variant="caption" color="text.secondary">
                {appDetail.description}
              </Typography>
            )}
          </Box>
        </Box>
      )}

      {isRegistryStack && hasUpdateAvailable && (
        <Alert
          severity={canOneClickUpdate ? "info" : "warning"}
          sx={{mb: 2}}
          action={
            canOneClickUpdate ? (
              <Button
                size="small"
                variant="outlined"
                onClick={() => setUpdateDialogOpen(true)}
                disabled={updateStackMutation.isPending}
              >
                Auf {latestVersion} updaten
              </Button>
            ) : undefined
          }
        >
          {canOneClickUpdate
            ? `Update verfügbar: ${stack.packageVersion ?? "–"} → ${latestVersion}`
            : `Update verfügbar (${latestVersion}), aber neue Environment-Variablen erkannt. Bitte über Konfiguration aktualisieren.`}
        </Alert>
      )}

      <Box sx={{display: "flex", gap: 3, flexWrap: "wrap"}}>
        {isRegistryStack ? (
          <>
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Package
              </Typography>
              <Box sx={{minHeight: 24, display: "flex", alignItems: "center"}}>
                <Typography variant="body2">{stack.packageId}</Typography>
              </Box>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Version
              </Typography>
              <Box sx={{minHeight: 24, display: "flex", alignItems: "center"}}>
                <Chip label={stack.packageVersion ?? "–"} size="small" variant="outlined" />
              </Box>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Registry
              </Typography>
              <Box sx={{minHeight: 24, display: "flex", alignItems: "center"}}>
                <Typography variant="body2">{stack.appRegistryName}</Typography>
              </Box>
            </Box>
          </>
        ) : (
          <Box>
            <Typography variant="caption" color="text.secondary" display="block">
              Quelle
            </Typography>
            <Chip
              label={isExternalStack ? "External" : "Custom Compose"}
              size="small"
              variant="outlined"
              sx={{color: "text.secondary", borderColor: "grey.400"}}
            />
          </Box>
        )}
        {!isExternalStack && (
          <>
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Erstellt
              </Typography>
              <Typography variant="body2">
                {stack.createdAt ? new Date(stack.createdAt).toLocaleString("de-DE") : "–"}
              </Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Aktualisiert
              </Typography>
              <Typography variant="body2">
                {stack.updatedAt ? new Date(stack.updatedAt).toLocaleString("de-DE") : "–"}
              </Typography>
            </Box>
          </>
        )}
      </Box>

      <UpdateStackDialog
        open={updateDialogOpen}
        currentVersion={stack.packageVersion}
        targetVersion={latestVersion}
        loading={updateStackMutation.isPending}
        onConfirm={handleUpdateConfirm}
        onClose={() => setUpdateDialogOpen(false)}
      />
    </>
  );
}
