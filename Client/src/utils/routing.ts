// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Routing utilities for microfrontend navigation
 *
 * Ensures all routes are prefixed with the mount path when the app
 * is integrated as a microfrontend.
 */

/**
 * Gets the mount path from environment variable, normalised to exactly one leading slash
 * and no trailing one. A value that already starts with a slash would otherwise yield
 * "//orchestrator", which browsers read as a protocol-relative URL to the host "orchestrator".
 */
export const getMountPath = (): string => {
  const mountPath = (import.meta.env.VITE_MOUNT_PATH || "").replace(/^\/+|\/+$/g, "");
  return mountPath ? `/${mountPath}` : "";
};

/**
 * Creates an absolute route with the mount path prefix
 * @param path - The relative path (should start with /)
 * @returns The absolute path with mount path prefix
 */
export const createRoute = (path: string): string => {
  const mountPath = getMountPath();
  // Ensure path starts with /
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${mountPath}${normalizedPath}`;
};
