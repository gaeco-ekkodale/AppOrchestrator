// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

export interface ProjectApp {
  /** Stable unique key: `${packageId}:${version}` */
  id: string;
  registryId: string;
  registryUrl: string;
  registryName: string;
  packageId: string;
  name: string;
  iconUrl?: string;
  version: string;
  stackName: string;
}

export interface DeploymentStep {
  id: string;
  apps: ProjectApp[];
}

export function makeAppId(packageId: string, version: string): string {
  return `${packageId}:${version}`;
}

export function makeProjectApp(params: {
  registryId: string;
  registryUrl: string;
  registryName: string;
  packageId: string;
  name: string;
  iconUrl?: string;
  version: string;
}): ProjectApp {
  const stackName = params.name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return {
    id: makeAppId(params.packageId, params.version),
    stackName,
    ...params,
  };
}
