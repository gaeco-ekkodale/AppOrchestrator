// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces;

/// <summary>
/// Proxy service that forwards HTTP requests to an external App Registry,
/// attaching the registry's stored API key and rewriting absolute URLs in
/// responses so the browser never reaches the registry directly.
/// </summary>
public interface IRegistryProxyService
{
    /// <summary>Returns all applications in the registry as a JSON byte array.</summary>
    Task<byte[]> GetApplicationsAsync(Guid registryId, CancellationToken ct = default);

    /// <summary>Returns a single application by packageId as a JSON byte array.</summary>
    Task<byte[]> GetApplicationAsync(Guid registryId, string packageId, CancellationToken ct = default);

    /// <summary>Returns all versions of a package as a JSON byte array.</summary>
    Task<byte[]> GetVersionsAsync(Guid registryId, string packageId, CancellationToken ct = default);

    /// <summary>Returns a single version as a JSON byte array.</summary>
    Task<byte[]> GetVersionAsync(Guid registryId, string packageId, string version, CancellationToken ct = default);

    /// <summary>
    /// Streams a file from the registry.
    /// Returns (content stream, content-type header value).
    /// The caller must dispose the stream.
    /// </summary>
    Task<(Stream Content, string ContentType)> GetFileAsync(
        Guid registryId, string packageId, string version, string fileName, CancellationToken ct = default);
}
