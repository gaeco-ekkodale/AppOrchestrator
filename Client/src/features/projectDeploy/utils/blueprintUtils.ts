// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import yaml from "js-yaml";

// ─── Blueprint v2 ────────────────────────────────────────────────────────────
// Instance-agnostic: identifies registries by URL, not internal IDs.
// Steps allow parallel-within-step deployment across any Orchestrator instance.

export interface BlueprintApp {
  /** URL of the app registry (e.g. https://registry.example.com) */
  registryUrl: string;
  packageId: string;
  version: string;
  stackName?: string;
  /** Optional non-secret env overrides */
  env?: Record<string, string>;
}

export interface BlueprintStep {
  apps: BlueprintApp[];
}

export interface Blueprint {
  project: {
    name: string;
    network?: string;
    /** v2: deployment steps (parallel within step, sequential across steps) */
    steps?: BlueprintStep[];
    /** v1 legacy: flat apps list — imported as a single step */
    apps?: BlueprintApp[];
  };
}

/**
 * Parse YAML blueprint from text.
 * Throws if YAML is invalid.
 */
export function parseBlueprint(yamlText: string): Blueprint {
  try {
    const parsed = yaml.load(yamlText) as unknown;
    if (!parsed || typeof parsed !== "object") {
      throw new Error("Blueprint muss ein YAML-Objekt sein");
    }
    const bp = parsed as Blueprint;
    if (!bp.project) {
      throw new Error("Blueprint muss einen 'project'-Schlüssel haben");
    }
    const hasSteps = Array.isArray(bp.project.steps);
    const hasApps = Array.isArray(bp.project.apps);
    if (!hasSteps && !hasApps) {
      throw new Error("Blueprint muss 'project.steps' oder 'project.apps' enthalten");
    }
    return bp;
  } catch (err) {
    if (err instanceof yaml.YAMLException) {
      throw new Error(`Ungültiges YAML: ${err.message}`);
    }
    throw err;
  }
}

/**
 * Normalise blueprint to always return steps (handles v1 flat apps list).
 */
export function getBlueprintSteps(bp: Blueprint): BlueprintStep[] {
  if (Array.isArray(bp.project.steps) && bp.project.steps.length > 0) {
    return bp.project.steps;
  }
  if (Array.isArray(bp.project.apps) && bp.project.apps.length > 0) {
    return [{ apps: bp.project.apps }];
  }
  return [];
}

/**
 * Export blueprint as YAML v2 (without secrets).
 */
export function exportBlueprint(
  projectName: string,
  networkName: string | undefined,
  steps: Array<{
    apps: Array<{
      registryUrl: string;
      packageId: string;
      version: string;
      stackName?: string;
      envOverrides?: Record<string, string>;
    }>;
  }>
): string {
  const blueprint: Blueprint = {
    project: {
      name: projectName,
      ...(networkName ? { network: networkName } : {}),
      steps: steps.map((step) => ({
        apps: step.apps.map((app) => ({
          registryUrl: app.registryUrl,
          packageId: app.packageId,
          version: app.version,
          stackName: app.stackName,
          ...(app.envOverrides && Object.keys(app.envOverrides).length > 0
            ? { env: app.envOverrides }
            : {}),
        })),
      })),
    },
  };

  const yamlStr = yaml.dump(blueprint, {
    lineWidth: -1,
    noRefs: true,
  });

  return (
    "# AppOrchestrator Project Blueprint v2\n" +
    "# Enthält keine Secrets — diese werden beim Import im Wizard abgefragt\n" +
    "# Funktioniert mit jeder AppOrchestrator-Instanz, die die referenzierten Registries kennt\n" +
    yamlStr
  );
}

/**
 * Validate blueprint structure. Returns list of error strings (empty = valid).
 */
export function validateBlueprintStructure(bp: Blueprint): string[] {
  const errors: string[] = [];

  if (!bp.project) {
    errors.push("Fehlender 'project'-Schlüssel");
    return errors;
  }

  if (!bp.project.name) {
    errors.push("Fehlender 'project.name'");
  }

  const steps = getBlueprintSteps(bp);
  if (steps.length === 0) {
    errors.push("Blueprint muss mindestens einen Schritt mit Apps enthalten");
    return errors;
  }

  steps.forEach((step, stepIdx) => {
    if (!Array.isArray(step.apps) || step.apps.length === 0) {
      errors.push(`Schritt ${stepIdx + 1}: muss mindestens eine App enthalten`);
      return;
    }
    step.apps.forEach((app, appIdx) => {
      if (!app.registryUrl) errors.push(`Schritt ${stepIdx + 1}, App ${appIdx + 1}: fehlende 'registryUrl'`);
      if (!app.packageId) errors.push(`Schritt ${stepIdx + 1}, App ${appIdx + 1}: fehlende 'packageId'`);
      if (!app.version) errors.push(`Schritt ${stepIdx + 1}, App ${appIdx + 1}: fehlende 'version'`);
    });
  });

  return errors;
}
