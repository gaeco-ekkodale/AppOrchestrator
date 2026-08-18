// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Models;

namespace AppOrchestrator.Api.Services._Interfaces.Stacks;

/// <summary>
/// HTTP client abstraction for fetching compose files from an external application registry API.
/// Encapsulates URL construction, API-key attachment, and HTTP execution so that
/// callers never handle HTTP infrastructure directly.
/// </summary>
public interface IAppRegistryClient
{
    /// <summary>
    /// Fetches the <c>docker-compose.yaml</c> for a specific package version from a registry.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    /// <param name="registry">The registry to fetch from. Its stored API key is used for authentication.</param>
    /// <param name="packageId">Package identifier within the registry.</param>
    /// <param name="version">Package version to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readable <see cref="Stream"/> containing the raw compose file content.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the registry endpoint returns a non-success status code or is unreachable.</exception>
    Task<Stream> FetchComposeFileAsync(
        AppRegistry registry,
        string packageId,
        string version,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads the full package ZIP for a specific version from a registry.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    /// <param name="registry">The registry to fetch from. Its stored API key is used for authentication.</param>
    /// <param name="packageId">Package identifier within the registry.</param>
    /// <param name="version">Package version to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readable <see cref="Stream"/> containing the raw ZIP archive content.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the registry endpoint returns a non-success status code or is unreachable.</exception>
    Task<Stream> DownloadPackageZipAsync(
        AppRegistry registry,
        string packageId,
        string version,
        CancellationToken ct = default);
}
