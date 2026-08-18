// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useState} from "react";

export interface StackSelection {
  selectedIds: string[];
  isSelected: (id: string) => boolean;
  /** Every visible stack is selected. */
  allSelected: boolean;
  /** Some — but not all — visible stacks are selected. */
  someSelected: boolean;
  toggle: (id: string) => void;
  toggleAll: () => void;
  clear: () => void;
}

/**
 * Multi-select state for the stacks table, keyed by docker project name.
 *
 * Everything is derived against the currently visible ids, so a filter change or a deleted
 * stack can never leak an invisible stack into a bulk action — the ids stay in state, but
 * only the visible ones count.
 */
export function useStackSelection(visibleIds: string[]): StackSelection {
  const [selected, setSelected] = useState<Set<string>>(new Set());

  // Keeps the visible order rather than insertion order, so bulk actions run top to bottom.
  const selectedIds = visibleIds.filter((id) => selected.has(id));

  const toggle = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });

  const toggleAll = () =>
    setSelected(
      selectedIds.length === visibleIds.length ? new Set() : new Set(visibleIds),
    );

  return {
    selectedIds,
    isSelected: (id: string) => selected.has(id),
    allSelected: visibleIds.length > 0 && selectedIds.length === visibleIds.length,
    someSelected: selectedIds.length > 0 && selectedIds.length < visibleIds.length,
    toggle,
    toggleAll,
    clear: () => setSelected(new Set()),
  };
}
