// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useMemo} from "react";
import {useQueries} from "@tanstack/react-query";
import {StackSource, type StackDTO, type AppRegistryDTO} from "@/api/orchestrator";
import {versionsQueryOptions} from "@/features/registryClient/queries";

interface Network {
  name?: string | null;
  allowedVersionSuffixes?: string[];
}

/**
 * Returns a map of dockerProjectName → newest eligible version, for every stack
 * that has a newer version available.
 * Uses the same version endpoint and allowedSuffixes logic as StackInfoSection.
 */
export function useUpdateAvailableStackIds(
  stacks: StackDTO[],
  registries: AppRegistryDTO[],
  networks: Network[],
): Map<string, string> {
  const registryIds = useMemo(() => new Set(registries.map((r) => r.id!)), [registries]);

  // One entry per unique (registry, package) pair found in installed APP_STORE stacks.
  const stackedPackages = useMemo(() => {
    const seen = new Set<string>();
    const result: {registryId: string; packageId: string}[] = [];
    stacks.forEach((s) => {
      if (s.source !== StackSource.APP_STORE || !s.appRegistryId || !s.packageId) return;
      const key = `${s.appRegistryId}:${s.packageId}`;
      if (seen.has(key)) return;
      seen.add(key);
      if (registryIds.has(s.appRegistryId))
        result.push({registryId: s.appRegistryId, packageId: s.packageId});
    });
    return result;
  }, [stacks, registryIds]);

  // Batch version fetch — same query keys as useAppVersions, so the cache is shared.
  const versionQueries = useQueries({
    queries: stackedPackages.map((p) => versionsQueryOptions(p.registryId, p.packageId)),
  });

  return useMemo(() => {
    const networkByName = new Map(networks.map((n) => [n.name, n]));
    const versionsMap = new Map(
      stackedPackages.map((p, i) => [
        `${p.registryId}:${p.packageId}`,
        versionQueries[i]?.data ?? [],
      ]),
    );

    const updates = new Map<string, string>();
    stacks.forEach((stack) => {
      if (stack.source !== StackSource.APP_STORE) return;
      if (!stack.dockerProjectName || !stack.appRegistryId || !stack.packageId) return;

      const network = stack.networkName ? networkByName.get(stack.networkName) : undefined;
      const suffixes = network?.allowedVersionSuffixes ?? [];
      const all = versionsMap.get(`${stack.appRegistryId}:${stack.packageId}`) ?? [];

      // Same filter as StackInfoSection: narrow to eligible versions, then pick newest.
      const eligible =
        suffixes.length === 0
          ? all
          : all.filter((v) => {
              const dash = v.version.indexOf("-");
              const preRelease = dash >= 0 ? v.version.slice(dash + 1).toLowerCase() : "";
              return suffixes.some((s) => (s === "" ? dash < 0 : preRelease === s.toLowerCase()));
            });

      const latest = eligible[0]?.version;
      if (!latest || !stack.packageVersion) return;
      if (latest === stack.packageVersion) return;
      updates.set(stack.dockerProjectName, latest);
    });
    return updates;
  }, [stacks, stackedPackages, versionQueries, networks]);
}
