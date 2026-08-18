// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState, useRef, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import {
  Container,
  Box,
  Button,
  Card,
  CardContent,
  Stepper,
  Step,
  StepLabel,
  TextField,
  MenuItem,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Divider,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import { useNetworks } from "@/features/networks/hooks/useNetworks";
import { useRegistries } from "@/features/appRegistries/hooks/useRegistries";
import { useStacks } from "@/features/stacks/hooks/useStacks";
import { getSharedVariablesForNetwork } from "@/features/networks/sharedVariables";
import {
  ProjectSecretsForm,
  ProjectDeployProgress,
} from "@/features/projectDeploy/components";
import { DeploymentStepsBoard } from "@/features/projectDeploy/components/DeploymentStepsBoard";
import {
  exportBlueprint,
  getBlueprintSteps,
  validateBlueprintStructure,
  type Blueprint,
} from "@/features/projectDeploy/utils/blueprintUtils";
import { useProjectDeploy, type ProjectAppConfig } from "@/features/projectDeploy/hooks/useProjectDeploy";
import { useProjectBasket } from "@/features/projectDeploy/context/ProjectBasketContext";
import { useMergedEnvSchemas } from "@/features/projectDeploy/hooks/useMergedEnvSchemas";
import { buildCompleteEnvConfig } from "@/features/projectDeploy/utils/envAggregation";
import type { DeploymentStep, ProjectApp } from "@/features/projectDeploy/types";
import { createRoute } from "@/utils/routing";
import { useToast } from "@/features/shared/contexts/ToastContext";

type WizardStep = "organize" | "config" | "deploy";

interface LocationState {
  blueprint?: Blueprint;
}

function ProjectDeployPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { showToast } = useToast();

  const { networks } = useNetworks();
  const { registries } = useRegistries();
  const { stacks: allStacks } = useStacks();
  const basket = useProjectBasket();

  const [wizardStep, setWizardStep] = useState<WizardStep>("organize");
  const [networkName, setNetworkName] = useState("");
  const [steps, setSteps] = useState<DeploymentStep[]>([]);
  const [secretValues, setSecretValues] = useState<Record<string, string>>({});
  const [blueprintDialogOpen, setBlueprintDialogOpen] = useState(false);
  const [blueprintYaml, setBlueprintYaml] = useState("");

  const initialized = useRef(false);

  // Initialize from blueprint (Projekt-Tab import) or from basket
  useEffect(() => {
    if (initialized.current) return;
    initialized.current = true;

    const state = location.state as LocationState | null;

    if (state?.blueprint) {
      const bp = state.blueprint;
      const errors = validateBlueprintStructure(bp);
      if (errors.length > 0) {
        showToast(`Blueprint-Fehler: ${errors.join(", ")}`, "error");
        navigate(createRoute("/store"), { replace: true });
        return;
      }

      const bpSteps = getBlueprintSteps(bp);
      const parsedSteps: DeploymentStep[] = bpSteps.map((bpStep) => ({
        id: crypto.randomUUID(),
        apps: bpStep.apps.map((app): ProjectApp => {
          const registry = registries.find((r) => r.baseUrl === app.registryUrl);
          return {
            id: `${app.packageId}:${app.version}`,
            registryId: registry?.id ?? "",
            registryUrl: app.registryUrl,
            registryName: registry?.name ?? app.registryUrl,
            packageId: app.packageId,
            name: app.packageId,
            version: app.version,
            stackName: app.stackName || app.packageId,
          };
        }),
      }));

      setSteps(parsedSteps);
      if (bp.project.network) setNetworkName(bp.project.network);
      setWizardStep("config");
    } else {
      setSteps([{ id: crypto.randomUUID(), apps: [...basket.apps] }]);
      setWizardStep("organize");
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const { deploy, statuses, isDeploying } = useProjectDeploy();

  const allApps = steps.flatMap((s) => s.apps);

  // Load schemas for all apps so we can include defaults in envConfig on deploy
  const { schemas } = useMergedEnvSchemas(
    allApps.map((a) => ({ registryId: a.registryId, packageId: a.packageId, version: a.version })),
  );

  const selectedNetwork = networks.find((n) => n.name === networkName);
  const sharedVars = getSharedVariablesForNetwork(selectedNetwork);
  const sharedVarKeys = new Set(sharedVars.map((v) => v.key));

  // Compute which packageIds are already deployed in the selected network
  const deployedPackageIds = new Set(
    allStacks
      .filter((s) => s.networkName === networkName && s.packageId)
      .map((s) => s.packageId as string),
  );

  const handleExportBlueprint = () => {
    const yamlContent = exportBlueprint(
      networkName || "blueprint",
      networkName || undefined,
      steps.map((step) => ({
        apps: step.apps.map((a) => ({
          registryUrl: a.registryUrl,
          packageId: a.packageId,
          version: a.version,
          stackName: a.stackName,
        })),
      })),
    );
    setBlueprintYaml(yamlContent);
    setBlueprintDialogOpen(true);
  };

  const handleDeploy = async () => {
    setWizardStep("deploy");

    // Convert shared vars array to Record for buildCompleteEnvConfig
    const sharedVarsRecord: Record<string, string> = {};
    sharedVars.forEach((v) => {
      sharedVarsRecord[v.key] = v.value;
    });

    // Build complete per-app env config: schema defaults + shared vars + user-filled secrets.
    const perAppEnvConfig = buildCompleteEnvConfig(schemas, secretValues, sharedVarsRecord);

    const stepConfigs: ProjectAppConfig[][] = steps.map((step) =>
      step.apps.map((app): ProjectAppConfig => ({
        id: app.id,
        registryId: app.registryId,
        packageId: app.packageId,
        version: app.version,
        stackName: app.stackName,
        // Use per-app config (includes schema defaults); fall back to secretValues if schema not loaded yet
        envConfig: perAppEnvConfig[app.packageId] ?? { ...secretValues },
      })),
    );

    try {
      await deploy(stepConfigs, networkName, networks);
      basket.clear();
    } catch (err) {
      showToast(
        `Deployment-Fehler: ${err instanceof Error ? err.message : "Unbekannter Fehler"}`,
        "error",
      );
    }
  };

  const canProceedOrganize = allApps.length > 0 && networkName !== "";
  const canProceedConfig = true; // secrets form validates internally

  const stepperIndex = wizardStep === "organize" ? 0 : wizardStep === "config" ? 1 : 2;

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Stepper activeStep={stepperIndex} sx={{ mb: 4 }}>
        <Step><StepLabel>Schritte planen</StepLabel></Step>
        <Step><StepLabel>Konfigurieren</StepLabel></Step>
        <Step><StepLabel>Deployen</StepLabel></Step>
      </Stepper>

      <Card sx={{ borderRadius: 2, mb: 3 }}>
        <CardContent sx={{ p: 3 }}>

          {/* ── Step 1: organize ── */}
          {wizardStep === "organize" && (
            <Box>
              {/* Environment selector at top of organize step */}
              <TextField
                select
                label="Ziel-Umgebung"
                value={networkName}
                onChange={(e) => setNetworkName(e.target.value)}
                fullWidth
                size="small"
                sx={{ mb: 3 }}
                helperText="Wähle die Umgebung — bereits deployte Apps werden markiert."
              >
                {networks.map((n) => (
                  <MenuItem key={n.name} value={n.name}>
                    {n.name}
                  </MenuItem>
                ))}
              </TextField>

              <DeploymentStepsBoard
                steps={steps}
                onStepsChange={setSteps}
                onExportBlueprint={handleExportBlueprint}
                onBackToStore={() => navigate(createRoute("/store"))}
                deployedPackageIds={networkName ? deployedPackageIds : undefined}
              />

              <Divider sx={{ my: 3 }} />

              <Box sx={{ display: "flex", gap: 1, justifyContent: "flex-end" }}>
                <Button
                  variant="contained"
                  endIcon={<ArrowForwardIcon />}
                  onClick={() => setWizardStep("config")}
                  disabled={!canProceedOrganize}
                >
                  Weiter
                </Button>
              </Box>
            </Box>
          )}

          {/* ── Step 2: config ── */}
          {wizardStep === "config" && (
            <Box>
              {/* Show chosen environment (read-only) */}
              <TextField
                select
                label="Umgebung"
                value={networkName}
                onChange={(e) => setNetworkName(e.target.value)}
                fullWidth
                sx={{ mb: 3 }}
              >
                {networks.map((n) => (
                  <MenuItem key={n.name} value={n.name}>
                    {n.name}
                  </MenuItem>
                ))}
              </TextField>

              <ProjectSecretsForm
                apps={allApps.map((a) => ({
                  registryId: a.registryId,
                  packageId: a.packageId,
                  version: a.version,
                  stackName: a.stackName,
                }))}
                values={secretValues}
                onChange={setSecretValues}
                sharedVarKeys={sharedVarKeys}
              />

              <Divider sx={{ my: 3 }} />

              <Box sx={{ display: "flex", gap: 1, justifyContent: "space-between" }}>
                <Button
                  variant="outlined"
                  startIcon={<ArrowBackIcon />}
                  onClick={() => setWizardStep("organize")}
                >
                  Zurück
                </Button>
                <Button
                  variant="contained"
                  endIcon={<CloudUploadIcon />}
                  onClick={handleDeploy}
                  disabled={!canProceedConfig || isDeploying}
                >
                  {isDeploying ? <CircularProgress size={24} /> : "Deployen"}
                </Button>
              </Box>
            </Box>
          )}

          {/* ── Step 3: deploy progress ── */}
          {wizardStep === "deploy" && (
            <ProjectDeployProgress
              statuses={statuses}
              isDeploying={isDeploying}
              onClose={() => navigate(createRoute("/stacks"))}
            />
          )}
        </CardContent>
      </Card>

      {/* Blueprint Export Dialog */}
      <Dialog
        open={blueprintDialogOpen}
        onClose={() => setBlueprintDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Blueprint exportieren</DialogTitle>
        <DialogContent>
          <TextField
            value={blueprintYaml}
            multiline
            minRows={10}
            fullWidth
            variant="outlined"
            size="small"
            slotProps={{ input: { readOnly: true } }}
            sx={{ mt: 2, fontFamily: "monospace" }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setBlueprintDialogOpen(false)}>Schließen</Button>
          <Button
            variant="contained"
            onClick={() => {
              const element = document.createElement("a");
              const file = new Blob([blueprintYaml], { type: "text/yaml" });
              element.href = URL.createObjectURL(file);
              element.download = `${networkName || "blueprint"}.yaml`;
              document.body.appendChild(element);
              element.click();
              document.body.removeChild(element);
              setBlueprintDialogOpen(false);
            }}
          >
            Download
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}

export default ProjectDeployPage;
