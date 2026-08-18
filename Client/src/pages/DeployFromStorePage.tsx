// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useEffect, useRef, useState} from "react";
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
  MenuItem,
  TextField,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import {useQuery} from "@tanstack/react-query";
import {useRegistries} from "@/features/appRegistries/hooks/useRegistries";
import {useNetworks} from "@/features/networks/hooks/useNetworks";
import {mergeNetworkSharedVariables} from "@/features/networks/sharedVariables";
import {AppDeployHeader, EnvSchemaForm, PackageFilesSection, isEnvSchemaValid} from "@/features/store/components";
import {useEnvSchema} from "@/features/registryClient/hooks/useEnvSchema";
import {useAppVersions} from "@/features/registryClient/hooks/useAppVersions";
import {appDetailQueryOptions} from "@/features/registryClient/queries";
import {useCreateStackMutation} from "@/features/stacks/hooks/useStackMutations";
import {LoadingSpinner} from "@/features/shared/components";
import {getSharedVariablesForNetwork} from "@/features/networks/sharedVariables";
import {createRoute} from "@/utils/routing";

function DeployFromStorePage() {
  const {registryId, packageId, version} = useParams<{
    registryId: string;
    packageId: string;
    version: string;
  }>();

  if (!registryId || !packageId || !version) {
    return (
      <Container maxWidth="md" sx={{py: 4}}>
        <Alert severity="error">Invalid URL parameters</Alert>
      </Container>
    );
  }

  return <DeployFromStoreContent registryId={registryId} packageId={packageId} version={version} />;
}

function DeployFromStoreContent({
  registryId,
  packageId,
  version,
}: {
  registryId: string;
  packageId: string;
  version: string;
}) {
  const {registries, isLoading: registriesLoading} = useRegistries();
  const {networks} = useNetworks();
  const navigate = useNavigate();

  const registry = registries.find((r) => r.id === registryId);

  const {data: app, isLoading: appLoading} = useQuery(appDetailQueryOptions(registryId, packageId));

  const {schema} = useEnvSchema(registryId, packageId, version);

  const {versions} = useAppVersions(registryId, packageId);
  const packageFiles = versions.find((v) => v.version === version)?.packageFiles ?? [];

  const [stackName, setStackName] = useState("");
  const [envValues, setEnvValues] = useState<Record<string, string>>({});
  const [networkName, setNetworkName] = useState("");
  const userEditedName = useRef(false);

  useEffect(() => {
    if (app?.name && !userEditedName.current) {
      const slug = app.name
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "");
      setStackName(slug);
    }
  }, [app?.name]);

  const deployMutation = useCreateStackMutation((data) => {
    navigate(createRoute(`/stacks/${data.dockerProjectName}`));
  });

  const handleDeploy = () => {
    deployMutation.mutate({
      stackName,
      registryId,
      packageId,
      version,
      envConfig: mergeNetworkSharedVariables(networkName, envValues, networks),
      networkName,
    });
  };

  if (registriesLoading || appLoading) return <LoadingSpinner />;

  if (!registry) {
    return (
      <Container maxWidth="md" sx={{py: 4}}>
        <Alert severity="error">Registry mit ID {registryId} wurde nicht gefunden.</Alert>
      </Container>
    );
  }

  const selectedNetwork = networks.find((n) => n.name === networkName);
  const currentSharedVars = getSharedVariablesForNetwork(selectedNetwork);
  const sharedKeySet = new Set(currentSharedVars.map((v) => v.key));

  const noEnvironments = networks.length === 0;
  const isValid =
    stackName.trim() !== "" &&
    networkName !== "" &&
    isEnvSchemaValid(schema, envValues, sharedKeySet);

  return (
    <Container maxWidth="md" sx={{py: 4}}>
      <Button
        component={Link}
        to={createRoute("/store")}
        startIcon={<ArrowBackIcon />}
        sx={{mb: 2}}
      >
        Zurück zum App Store
      </Button>

      <AppDeployHeader
        iconUrl={app?.iconUrl}
        name={app?.name}
        packageId={packageId}
        version={version}
        registryName={registry.name}
      />

      <Card sx={{borderRadius: 2}}>
        <CardContent sx={{p: 3}}>
          {noEnvironments && (
            <Alert
              severity="warning"
              sx={{mb: 3}}
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
              Zuerst ein Environment erstellen, bevor ein Stack deployed werden kann.
            </Alert>
          )}

          <Typography variant="h6" fontWeight="bold" gutterBottom>
            Stack-Konfiguration
          </Typography>
          <Divider sx={{mb: 2}} />
          <Box sx={{display: "flex", flexDirection: "column", gap: 2, mb: 1}}>
            <TextField
              label="Stack-Name"
              value={stackName}
              onChange={(e) => {
                userEditedName.current = true;
                setStackName(e.target.value);
              }}
              fullWidth
              required
              helperText="Eindeutiger Name für diesen Stack (z. B. myapp-prod)"
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
          </Box>

          <Divider sx={{my: 3}} />

          <PackageFilesSection files={packageFiles} />

          <EnvSchemaForm
            registryId={registryId}
            packageId={packageId}
            version={version}
            values={envValues}
            onChange={setEnvValues}
            sharedVariables={currentSharedVars}
            networkName={networkName}
          />

          <Divider sx={{my: 3}} />

          <Box sx={{display: "flex", justifyContent: "flex-end"}}>
            <Button
              variant="contained"
              size="large"
              startIcon={
                deployMutation.isPending ? (
                  <CircularProgress size={18} color="inherit" />
                ) : (
                  <CloudUploadIcon />
                )
              }
              onClick={handleDeploy}
              disabled={!isValid || deployMutation.isPending}
            >
              Deployen
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
}

export default DeployFromStorePage;
