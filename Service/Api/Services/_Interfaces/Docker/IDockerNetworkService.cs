// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces.Docker;

/// <summary>
/// Provides Docker network lifecycle operations.
///
/// Implementations talk directly to the Docker daemon.
/// Business logic (DB persistence, validation) lives in the calling endpoint.
/// </summary>
public interface IDockerNetworkService
{
    /// <summary>
    /// Creates a Docker bridge network with the given name.
    /// The name is both the display label and the stable Docker network identifier.
    /// </summary>
    /// <param name="networkName">Name of the network to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateNetworkAsync(string networkName, CancellationToken ct = default);

    /// <summary>
    /// Removes a Docker network by name.
    /// Does nothing and logs a warning if the network no longer exists in Docker.
    /// </summary>
    /// <param name="networkName">The name of the Docker network to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteNetworkAsync(string networkName, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when the Docker network has one or more containers attached.
    /// Returns <c>false</c> when the network does not exist in Docker or has no containers.
    /// </summary>
    /// <param name="networkName">Name of the Docker network to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> HasContainersAsync(string networkName, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when a Docker network with the given name already exists.
    /// </summary>
    Task<bool> ExistsAsync(string networkName, CancellationToken ct = default);
}
