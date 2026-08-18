// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import type { EnvSchemaField } from "@/features/registryClient/registryApiClient";

export interface AggregatedField {
  name: string;
  label: string;
  description: string;
  type: "text" | "password" | "select" | "boolean";
  required: boolean;
  default: string | boolean | undefined;
  options: string[];
  appliesTo: string[]; // list of packageIds that need this field
}

/**
 * Aggregates env schemas from multiple apps.
 * Deduplicates fields by name; a field appears once if it's required + no default + not in shared vars.
 * The appliesTo list shows which packages need this field.
 */
export function aggregateEnvSchemas(
  appSchemas: Record<
    string,
    { schema: EnvSchemaField[]; packageId: string }
  >,
  sharedVarKeys: Set<string>
): AggregatedField[] {
  const fieldsMap = new Map<string, AggregatedField>();

  for (const [, { schema, packageId }] of Object.entries(appSchemas)) {
    for (const field of schema) {
      // Skip fields that have defaults or are provided by network shared vars
      if (field.default !== undefined && field.default !== null && field.default !== "") {
        continue;
      }
      if (sharedVarKeys.has(field.name)) {
        continue;
      }
      if (!field.required) {
        continue;
      }

      // Field is a true "secret" — required, no default, not shared
      if (fieldsMap.has(field.name)) {
        // Already seen this field name, just add the packageId to appliesTo
        const existing = fieldsMap.get(field.name)!;
        if (!existing.appliesTo.includes(packageId)) {
          existing.appliesTo.push(packageId);
        }
      } else {
        // New field
        fieldsMap.set(field.name, {
          name: field.name,
          label: field.label || field.name,
          description: field.description || "",
          type: field.type,
          required: true,
          default: undefined,
          options: field.options || [],
          appliesTo: [packageId],
        });
      }
    }
  }

  return Array.from(fieldsMap.values()).sort((a, b) =>
    a.name.localeCompare(b.name)
  );
}

/**
 * For each app, pre-fill all env vars that have defaults or are in shared vars.
 * The caller only needs to provide values for the aggregated "secret" fields.
 */
export function buildCompleteEnvConfig(
  appSchemas: Record<
    string,
    { schema: EnvSchemaField[]; packageId: string }
  >,
  secretValues: Record<string, string>,
  networkSharedVars: Record<string, string>
): Record<string, Record<string, string>> {
  const result: Record<string, Record<string, string>> = {};

  for (const [, { schema, packageId }] of Object.entries(appSchemas)) {
    const appEnv: Record<string, string> = { ...networkSharedVars };

    for (const field of schema) {
      const fieldName = field.name;

      // Use secret value if provided (highest priority)
      if (secretValues[fieldName]) {
        appEnv[fieldName] = secretValues[fieldName];
        continue;
      }

      // If already in network shared vars, keep it (don't override with default)
      if (networkSharedVars[fieldName]) {
        continue;
      }

      // Use default if available and not in shared vars
      if (field.default !== undefined && field.default !== null) {
        appEnv[fieldName] = String(field.default);
        continue;
      }

      // Otherwise leave empty — optional field with no default
    }

    result[packageId] = appEnv;
  }

  return result;
}
