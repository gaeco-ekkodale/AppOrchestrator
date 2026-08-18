// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Registry API client — proxied through the Orchestrator.
 *
 * All calls go to the Orchestrator backend (/api/app-registries/{registryId}/...)
 * which forwards them to the external App Registry using the stored API key.
 * The browser never contacts the registry directly.
 */
import * as jsYaml from "js-yaml";
import { request } from "@/api/orchestrator/core/request";
import { OpenAPI } from "@/api/orchestrator/core/OpenAPI";
import type { AppRegistryDTO } from "@/api/orchestrator";

// ─── Registry DTO Types ───────────────────────────────────────────────────────
// The Orchestrator proxy returns the same JSON shape as the Registry API.

export interface ApplicationDTO {
  packageId: string;
  name: string;
  description?: string;
  iconUrl?: string;
  ownerUsername?: string;
  repositoryUrl?: string;
  documentationUrl?: string;
  tags?: string[];
  defaultVersion?: string;
}

/** A data file the package ships and mounts into its containers at deploy time. */
export interface PackageFileDTO {
  name: string;
  description?: string;
  downloadUrl: string;
}

export interface ApplicationVersionDTO {
  version: string;
  packageId?: string;
  description?: string;
  readmeUrl?: string;
  manifestUrl?: string;
  createdAt: string;
  downloads?: number;
  isPreRelease?: boolean;
  isDeprecated?: boolean;
  isPrivate?: boolean;
  dependencies?: Array<{ name: string; version: string }>;
  packageFiles?: PackageFileDTO[];
}

// ─── Env Schema Types ────────────────────────────────────────────────────────

export interface EnvSchemaField {
  name: string;
  label: string;
  description?: string;
  type: "text" | "password" | "select" | "boolean";
  required?: boolean;
  default?: string;
  options?: string[];
}

export type AppWithRegistry = ApplicationDTO & {
  registryId: string;
  registryName: string;
  registryBaseUrl: string;
};

/** Fetch all public apps from a single registry via the Orchestrator proxy. */
export async function fetchAppsFromRegistry(
  registry: AppRegistryDTO,
): Promise<AppWithRegistry[]> {
  const apps = await (request(OpenAPI, {
    method: "GET",
    url: "/api/app-registries/{registryId}/applications",
    path: { registryId: registry.id },
    errors: {
      401: "Not authenticated.",
      404: "Registry not found.",
      502: "Upstream registry error.",
    },
  }) as Promise<ApplicationDTO[]>);

  return apps.map((app) => ({
    ...app,
    registryId: registry.id!,
    registryName: registry.name!,
    registryBaseUrl: registry.baseUrl!,
  }));
}

/** Fetch all versions for a package from a specific registry via the Orchestrator proxy. */
export function fetchVersionsFromRegistry(
  registryId: string,
  packageId: string,
): Promise<ApplicationVersionDTO[]> {
  return request(OpenAPI, {
    method: "GET",
    url: "/api/app-registries/{registryId}/applications/{packageId}/versions",
    path: { registryId, packageId },
    errors: {
      401: "Not authenticated.",
      404: "Registry or application not found.",
      502: "Upstream registry error.",
    },
  }) as Promise<ApplicationVersionDTO[]>;
}

/** Fetch a single app from a specific registry via the Orchestrator proxy. */
export function fetchSingleAppFromRegistry(
  registryId: string,
  packageId: string,
): Promise<ApplicationDTO> {
  return request(OpenAPI, {
    method: "GET",
    url: "/api/app-registries/{registryId}/applications/{packageId}",
    path: { registryId, packageId },
    errors: {
      401: "Not authenticated.",
      404: "Registry or application not found.",
      502: "Upstream registry error.",
    },
  }) as Promise<ApplicationDTO>;
}

/**
 * Fetch and parse the `.env.schema.yaml` for a specific app version via the Orchestrator proxy.
 */
export async function fetchEnvSchema(
  registryId: string,
  packageId: string,
  version: string,
): Promise<EnvSchemaField[]> {
  const token =
    typeof OpenAPI.TOKEN === "function"
      ? await OpenAPI.TOKEN({} as Parameters<typeof OpenAPI.TOKEN>[0])
      : OpenAPI.TOKEN;

  const base = (OpenAPI.BASE ?? "").replace(/\/$/, "");
  const url = `${base}/api/app-registries/${encodeURIComponent(registryId)}/applications/${encodeURIComponent(packageId)}/versions/${encodeURIComponent(version)}/files/.env.schema.yaml`;

  const response = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new Error(`Schema-Fetch fehlgeschlagen: ${response.status}`);
  }

  const text = await response.text();
  const parsed = jsYaml.load(text) as { envSchema?: EnvSchemaField[] };
  return parsed?.envSchema ?? [];
}
