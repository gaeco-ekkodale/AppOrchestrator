// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { OpenAPI } from "@/api/orchestrator/core/OpenAPI";

export const getAppVersionDownloadUrl = (
  registryId: string,
  packageId: string,
  version: string,
): string => {
  const base = (OpenAPI.BASE ?? "").replace(/\/$/, "");
  return `${base}/api/app-registries/${encodeURIComponent(registryId)}/applications/${encodeURIComponent(packageId)}/versions/${encodeURIComponent(version)}/files/docker-compose.yaml`;
};
