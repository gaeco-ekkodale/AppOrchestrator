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
import {useQueryClient} from "@tanstack/react-query";
import {StacksService} from "@/api/orchestrator";
import {useToast} from "@/features/shared/contexts/ToastContext";
import {stacksQueryKeys} from "@/features/stacks/queries";
import {getApiErrorMessage} from "@/utils/errorMessages";

export type BulkStackAction = "start" | "stop" | "restart" | "delete" | "deleteVolumes";

export interface BulkProgress {
  action: BulkStackAction;
  /** Number of stacks already processed. */
  done: number;
  total: number;
  /** Docker project name of the stack currently being processed. */
  currentId: string;
}

interface ActionDef {
  run: (id: string) => Promise<unknown>;
  /** "3 Stacks wurden …" */
  doneLabel: string;
  /** Shown while the action is running: "Stoppe 2 / 5 …" */
  runningLabel: string;
  errorLabel: string;
}

const ACTIONS: Record<BulkStackAction, ActionDef> = {
  start: {
    run: (id) => StacksService.startStack(id),
    doneLabel: "gestartet",
    runningLabel: "Starte",
    errorLabel: "Fehler beim Starten",
  },
  stop: {
    run: (id) => StacksService.stopStack(id),
    doneLabel: "gestoppt",
    runningLabel: "Stoppe",
    errorLabel: "Fehler beim Stoppen",
  },
  restart: {
    run: (id) => StacksService.restartStack(id),
    doneLabel: "neu gestartet",
    runningLabel: "Starte neu",
    errorLabel: "Fehler beim Neustart",
  },
  delete: {
    run: (id) => StacksService.deleteStack(id),
    doneLabel: "gelöscht",
    runningLabel: "Lösche",
    errorLabel: "Fehler beim Löschen",
  },
  deleteVolumes: {
    run: (id) => StacksService.deleteStackVolumes(id),
    doneLabel: "Volumes gelöscht",
    runningLabel: "Lösche Volumes von",
    errorLabel: "Fehler beim Löschen der Volumes",
  },
};

export function bulkActionRunningLabel({action, done, total}: BulkProgress): string {
  return `${ACTIONS[action].runningLabel} ${done + 1} / ${total} …`;
}

function stackCount(n: number): string {
  return n === 1 ? "1 Stack" : `${n} Stacks`;
}

/**
 * Applies one lifecycle action to many stacks.
 *
 * The stacks are processed one after another on purpose: every action ends up as a
 * `docker compose` invocation on the daemon, and firing a dozen of them at once makes the
 * whole host crawl. A failing stack doesn't abort the run — everything is reported in a
 * single summary toast at the end.
 */
export function useBulkStackActions() {
  const {showToast} = useToast();
  const queryClient = useQueryClient();
  const [progress, setProgress] = useState<BulkProgress | null>(null);

  const run = async (action: BulkStackAction, ids: string[]) => {
    if (progress !== null || ids.length === 0) return;

    const failedIds: string[] = [];
    let firstError: unknown;

    for (const [index, id] of ids.entries()) {
      setProgress({action, done: index, total: ids.length, currentId: id});
      try {
        await ACTIONS[action].run(id);
      } catch (err) {
        failedIds.push(id);
        firstError ??= err;
      }
      // Keep the list in sync while the run is still going, so statuses update live.
      queryClient.invalidateQueries({queryKey: stacksQueryKeys.all});
      queryClient.invalidateQueries({queryKey: stacksQueryKeys.detail(id)});
    }

    setProgress(null);

    const {doneLabel, errorLabel} = ACTIONS[action];
    const succeeded = ids.length - failedIds.length;

    if (failedIds.length === 0) {
      showToast(`${stackCount(succeeded)} ${doneLabel}`, "success");
      return;
    }

    const reason = getApiErrorMessage(firstError, errorLabel);
    if (succeeded === 0) {
      showToast(`${errorLabel} (${stackCount(failedIds.length)}): ${reason}`, "error");
      return;
    }

    showToast(
      `${stackCount(succeeded)} ${doneLabel}, ${failedIds.length} fehlgeschlagen: ${reason}`,
      "warning",
    );
  };

  return {run, progress, isRunning: progress !== null};
}
