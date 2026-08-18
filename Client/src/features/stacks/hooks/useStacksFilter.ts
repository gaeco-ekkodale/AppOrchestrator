// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useMemo, useState} from "react";
import {StackSource} from "@/api/orchestrator";
import {useStacks} from "./useStacks";
import {useAllApps} from "@/features/registryClient/hooks/useAllApps";
import {useNetworks} from "@/features/networks/hooks/useNetworks";
import {useRegistries} from "@/features/appRegistries/hooks/useRegistries";
import {useUpdateAvailableStackIds} from "./useUpdateAvailableStackIds";

const SESSION_KEY = "stacks-filter";

interface StoredFilter {
  q: string;
  source: "all" | "managed" | "external";
  env: string;
}

function loadFilter(): StoredFilter {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (raw) return JSON.parse(raw) as StoredFilter;
  } catch {
    // ignore
  }
  return {q: "", source: "managed", env: ""};
}

function saveFilter(f: StoredFilter) {
  try {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(f));
  } catch {
    // ignore
  }
}

export function useStacksFilter() {
  const {stacks, isLoading, error} = useStacks();
  const {apps} = useAllApps();
  const {networks} = useNetworks();
  const {registries} = useRegistries();

  const appIconLookup = useMemo(() => {
    const map = new Map<string, string>();
    apps.forEach((a) => {
      if (a.iconUrl) map.set(`${a.registryId}:${a.packageId}`, a.iconUrl);
    });
    return map;
  }, [apps]);

  const updateAvailableStackIds = useUpdateAvailableStackIds(stacks, registries, networks);

  const initial = useMemo(loadFilter, []);
  const [search, setSearchRaw] = useState(initial.q);
  const [sourceFilter, setSourceFilterRaw] = useState(initial.source);
  const [environmentFilter, setEnvironmentFilterRaw] = useState(initial.env);

  const setSearch = (v: string) => {
    setSearchRaw(v);
    saveFilter({q: v, source: sourceFilter, env: environmentFilter});
  };
  const setSourceFilter = (v: "all" | "managed" | "external") => {
    setSourceFilterRaw(v);
    saveFilter({q: search, source: v, env: environmentFilter});
  };
  const setEnvironmentFilter = (v: string) => {
    setEnvironmentFilterRaw(v);
    saveFilter({q: search, source: sourceFilter, env: v});
  };

  const filtered = stacks.filter((s) => {
    if (sourceFilter === "managed" && s.source === StackSource.EXTERNAL) return false;
    if (sourceFilter === "external" && s.source !== StackSource.EXTERNAL) return false;
    if (environmentFilter === "__none__") {
      if (s.networkName) return false;
    } else if (environmentFilter) {
      if (s.networkName !== environmentFilter) return false;
    }
    if (search) {
      const q = search.toLowerCase();
      return (
        (s.stackName ?? "").toLowerCase().includes(q) ||
        (s.packageId ?? "").toLowerCase().includes(q) ||
        (s.appRegistryName ?? "").toLowerCase().includes(q) ||
        (s.dockerProjectName ?? "").toLowerCase().includes(q)
      );
    }
    return true;
  });

  return {
    stacks,
    filtered,
    isLoading,
    error,
    networks,
    appIconLookup,
    updateAvailableStackIds,
    search,
    setSearch,
    sourceFilter,
    setSourceFilter,
    environmentFilter,
    setEnvironmentFilter,
  };
}
