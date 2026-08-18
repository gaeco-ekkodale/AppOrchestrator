// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {type StackDTO, StackSource, StackStatus} from "@/api/orchestrator";

/**
 * Which lifecycle action a stack currently accepts, derived from its status.
 *
 * The API serialises StackStatus as a string ("Running"), so the status must be compared
 * against the enum — comparing it to the numeric enum positions silently matches nothing.
 */

/** Statuses in which the stack is mid-operation and must not be touched. */
export function isTransitioning(status?: StackStatus): boolean {
  return status === StackStatus.INSTALLING || status === StackStatus.UPDATING;
}

/** Running or partially running — i.e. at least one container is up. */
export function isRunning(status?: StackStatus): boolean {
  return status === StackStatus.RUNNING || status === StackStatus.PARTIAL;
}

export function canStart(status?: StackStatus): boolean {
  return !isTransitioning(status) && !isRunning(status);
}

export function canStop(status?: StackStatus): boolean {
  return (
    !isTransitioning(status) &&
    status !== StackStatus.STOPPED &&
    status !== StackStatus.UNKNOWN
  );
}

export function canRestart(status?: StackStatus): boolean {
  return canStop(status);
}

/** Update, clone and delete are only blocked while the stack is mid-operation. */
export function canMutate(status?: StackStatus): boolean {
  return !isTransitioning(status);
}

/** External stacks aren't managed by us, so their volumes stay untouched. */
export function canDeleteVolumes(stack: Pick<StackDTO, "status" | "source">): boolean {
  return canMutate(stack.status) && stack.source !== StackSource.EXTERNAL;
}
