// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useNetworks} from "./useNetworks";
import {useUpdateNetworkMutation} from "./useNetworkMutations";
import {
  getSharedVariablesForNetwork,
  toNetworkEnvironmentVariables,
} from "@/features/networks/sharedVariables";

/**
 * Promotes a single variable to the shared variables of one network.
 *
 * The update endpoint uses replace semantics, so the current shared variables and version
 * suffixes are read from the cached network and sent back together with the new entry.
 */
export function useAddSharedVariable(networkName?: string) {
  const {networks} = useNetworks();
  const network = networks.find((n) => n.name === (networkName ?? "").trim());

  const mutation = useUpdateNetworkMutation(
    undefined,
    "Variable zu den geteilten Variablen hinzugefügt",
  );

  const add = (key: string, value: string, onAdded?: () => void) => {
    const trimmedKey = key.trim();
    if (!network?.name || !trimmedKey) return;

    const others = getSharedVariablesForNetwork(network).filter((v) => v.key !== trimmedKey);

    mutation.mutate(
      {
        name: network.name,
        environmentVariables: toNetworkEnvironmentVariables([
          ...others,
          {key: trimmedKey, value},
        ]),
        allowedVersionSuffixes: network.allowedVersionSuffixes ?? [],
      },
      {onSuccess: () => onAdded?.()},
    );
  };

  return {
    add,
    /** False while no environment is selected — nothing to add the variable to. */
    canAdd: !!network?.name,
    isPending: mutation.isPending,
  };
}
