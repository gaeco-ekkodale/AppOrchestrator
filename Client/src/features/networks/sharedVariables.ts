// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import type { EnvironmentVariableInput, NetworkDTO } from "@/api/orchestrator";

export interface NetworkSharedVariable {
  key: string;
  value: string;
}

function toRecord(entries: NetworkSharedVariable[]): Record<string, string> {
  const result: Record<string, string> = {};
  entries.forEach(({ key, value }) => {
    const normalizedKey = key.trim();
    if (!normalizedKey) return;
    result[normalizedKey] = value;
  });
  return result;
}

function toEntries(record: Record<string, string>): NetworkSharedVariable[] {
  return Object.entries(record).map(([key, value]) => ({ key, value }));
}

export function getSharedVariablesForNetwork(
  network?: Pick<NetworkDTO, "environmentVariables"> | null,
): NetworkSharedVariable[] {
  if (!network?.environmentVariables?.length) return [];
  const variables = network.environmentVariables
    .filter((entry) => (entry.name ?? "").trim())
    .map((entry) => ({
      key: (entry.name ?? "").trim(),
      value: entry.value ?? "",
    }));
  return toEntries(toRecord(variables));
}

export function toNetworkEnvironmentVariables(
  entries: NetworkSharedVariable[],
): EnvironmentVariableInput[] {
  return entries
    .filter(({ key }) => key.trim())
    .map(({ key, value }) => ({
      name: key.trim(),
      value,
    }));
}

export function mergeNetworkSharedVariables(
  networkName: string | null | undefined,
  explicitValues: Record<string, string>,
  networks: Pick<NetworkDTO, "name" | "environmentVariables">[] = [],
): Record<string, string> | undefined {
  const normalizedName = (networkName ?? "").trim();
  const network = networks.find((item) => item.name === normalizedName);
  const shared = toRecord(getSharedVariablesForNetwork(network));
  const merged = { ...shared, ...explicitValues };
  return Object.keys(merged).length > 0 ? merged : undefined;
}
