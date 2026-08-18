// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

type UnknownError = {
  message?: unknown;
  statusText?: unknown;
  body?: unknown;
};

function asString(value: unknown): string | undefined {
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

function fromValidationErrors(value: unknown): string | undefined {
  if (!value || typeof value !== "object") return undefined;

  const errors = (value as { errors?: Record<string, string[]> }).errors;
  if (!errors || typeof errors !== "object") return undefined;

  const messages = Object.values(errors)
    .flatMap((items) => (Array.isArray(items) ? items : []))
    .map((entry) => asString(entry))
    .filter((entry): entry is string => Boolean(entry));

  return messages.length > 0 ? messages.join(" | ") : undefined;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!error) return fallback;

  const direct = asString(error);
  if (direct) return direct;

  const candidate = error as UnknownError;

  const body = candidate.body;
  const bodyString = asString(body);
  if (bodyString) return bodyString;

  if (body && typeof body === "object") {
    const structuredBody = body as {
      error?: unknown;
      message?: unknown;
      title?: unknown;
      detail?: unknown;
    };
    const fromBody =
      asString(structuredBody.error) ??
      fromValidationErrors(structuredBody) ??
      asString(structuredBody.message) ??
      asString(structuredBody.title) ??
      asString(structuredBody.detail);

    if (fromBody) return fromBody;
  }

  if (error instanceof Error) {
    const fromError = asString(error.message);
    if (fromError && fromError.toLowerCase() !== "generic error")
      return fromError;
  }

  const fromCandidate =
    asString(candidate.message) ?? asString(candidate.statusText);
  return fromCandidate ?? fallback;
}
