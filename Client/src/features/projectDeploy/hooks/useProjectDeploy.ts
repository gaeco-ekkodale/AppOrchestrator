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
import { useToast } from "@/features/shared/contexts/ToastContext";
import { StacksService } from "@/api/orchestrator/services/StacksService";
import { mergeNetworkSharedVariables } from "@/features/networks/sharedVariables";
import type { NetworkDTO } from "@/api/orchestrator/models/NetworkDTO";

export interface ProjectAppConfig {
  /** Stable id: `${packageId}:${version}` */
  id: string;
  registryId: string;
  packageId: string;
  version: string;
  stackName: string;
  envConfig: Record<string, string>;
}

export interface DeployStatus {
  id: string;
  packageId: string;
  stackName: string;
  stepIndex: number;
  status: "waiting" | "deploying" | "done" | "error" | "rolling-back" | "rolled-back";
  error?: string;
  dockerProjectName?: string;
}

/**
 * Orchestrates multi-step project deployment.
 * - Steps run sequentially.
 * - Apps within a step run in parallel (Promise.allSettled).
 * - On any failure: fully parallel rollback of ALL deployed stacks.
 */
export function useProjectDeploy() {
  const [statuses, setStatuses] = useState<DeployStatus[]>([]);
  const [isDeploying, setIsDeploying] = useState(false);
  const { showToast } = useToast();

  const updateStatus = (id: string, update: Partial<DeployStatus>) => {
    setStatuses((prev) =>
      prev.map((s) => (s.id === id ? { ...s, ...update } : s)),
    );
  };

  const deploy = async (
    steps: ProjectAppConfig[][],
    networkName: string,
    networks: NetworkDTO[],
  ) => {
    const allApps = steps.flat();
    setIsDeploying(true);
    setStatuses(
      allApps.map((a) => ({
        id: a.id,
        packageId: a.packageId,
        stackName: a.stackName,
        stepIndex: steps.findIndex((step) => step.some((app) => app.id === a.id)),
        status: "waiting",
      })),
    );

    // Track successfully deployed stacks for potential rollback
    const deployedStacks: Array<{ id: string; dockerProjectName: string }> = [];
    let failed = false;

    for (let stepIdx = 0; stepIdx < steps.length; stepIdx++) {
      const stepApps = steps[stepIdx];
      if (stepApps.length === 0) continue;

      // Mark all apps in this step as deploying
      stepApps.forEach((app) => updateStatus(app.id, { status: "deploying" }));

      // Deploy all apps in this step in parallel
      const results = await Promise.allSettled(
        stepApps.map(async (app) => {
          const mergedEnv = mergeNetworkSharedVariables(
            networkName,
            app.envConfig,
            networks,
          );

          const result = await StacksService.createStack({
            stackName: app.stackName,
            registryId: app.registryId,
            packageId: app.packageId,
            version: app.version,
            envConfig: mergedEnv,
            networkName,
          });

          return { app, dockerProjectName: result.dockerProjectName };
        }),
      );

      // Process results
      let stepFailed = false;
      for (const result of results) {
        if (result.status === "fulfilled") {
          const { app, dockerProjectName } = result.value;
          updateStatus(app.id, { status: "done", dockerProjectName });
          if (dockerProjectName) {
            deployedStacks.push({ id: app.id, dockerProjectName });
          }
        } else {
          const appIndex = results.indexOf(result);
          const app = stepApps[appIndex];
          const errorMsg = result.reason instanceof Error ? result.reason.message : "Unbekannter Fehler";
          updateStatus(app.id, { status: "error", error: errorMsg });
          showToast(`Deployment fehlgeschlagen für ${app.stackName}: ${errorMsg}`, "error");
          stepFailed = true;
          failed = true;
        }
      }

      if (stepFailed) break; // Stop processing further steps
    }

    if (failed) {
      await rollback(deployedStacks);
      setIsDeploying(false);
      return;
    }

    setIsDeploying(false);
    showToast("Projekt erfolgreich deployt!", "success");
  };

  const rollback = async (
    deployedStacks: Array<{ id: string; dockerProjectName: string }>,
  ) => {
    if (deployedStacks.length === 0) return;

    showToast("Rollback wird durchgeführt...", "warning");

    // Mark all as rolling-back in parallel
    setStatuses((prev) =>
      prev.map((s) =>
        deployedStacks.some((d) => d.id === s.id)
          ? { ...s, status: "rolling-back" }
          : s,
      ),
    );

    // Rollback all stacks in parallel
    await Promise.allSettled(
      deployedStacks.map(async ({ id, dockerProjectName }) => {
        try {
          await StacksService.deleteStack(dockerProjectName);
          updateStatus(id, { status: "rolled-back" });
        } catch (err) {
          const errorMsg = err instanceof Error ? err.message : "Unbekannter Fehler";
          showToast(`Rollback fehlgeschlagen für ${dockerProjectName}: ${errorMsg}`, "error");
        }
      }),
    );

    showToast("Rollback abgeschlossen", "info");
  };

  return { deploy, statuses, isDeploying };
}
