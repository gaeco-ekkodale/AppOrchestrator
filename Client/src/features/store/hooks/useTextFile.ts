// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useQuery } from "@tanstack/react-query";
import { OpenAPI } from "@/api/orchestrator/core/OpenAPI";

async function fetchRegistryFile(url: string): Promise<Response> {
  const token =
    typeof OpenAPI.TOKEN === "function"
      ? await OpenAPI.TOKEN({} as any)
      : OpenAPI.TOKEN;

  const absoluteUrl = url.startsWith("/") ? `${OpenAPI.BASE}${url}` : url;
  const res = await fetch(absoluteUrl, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res;
}

/**
 * Downloads a registry file to disk. Goes through fetch rather than a plain link because the
 * proxy endpoint requires the bearer token.
 */
export async function downloadRegistryFile(url: string, fileName: string): Promise<void> {
  const blob = await (await fetchRegistryFile(url)).blob();
  const objectUrl = URL.createObjectURL(blob);

  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = fileName;
  anchor.click();

  URL.revokeObjectURL(objectUrl);
}

/**
 * Loads a registry file as text through the Orchestrator proxy, which supplies the API key.
 * Used for readmes and for previewing package files before deployment.
 */
export function useTextFile(url: string | null | undefined, enabled = true) {
  return useQuery({
    queryKey: ["registryTextFile", url],
    queryFn: async () => (await fetchRegistryFile(url!)).text(),
    enabled: !!url && enabled,
    staleTime: 5 * 60 * 1000,
  });
}
