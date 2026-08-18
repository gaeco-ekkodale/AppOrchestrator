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
/// Provides Docker container registry authentication operations.
///
/// This interface encapsulates login, logout, and credential probe operations
/// against external container registries. All Docker-specific error handling
/// is encapsulated here so that callers (endpoints) remain free of direct
/// Docker SDK dependencies.
/// </summary>
public interface IDockerRegistryService
{
    /// <summary>
    /// Authenticates with a container registry using the Docker Engine
    /// <c>System.AuthenticateAsync</c> API.
    /// Returns a tuple containing a success flag and a human-readable outcome message.
    /// </summary>
    /// <param name="serverAddress">Registry server address, e.g. <c>ghcr.io</c>.</param>
    /// <param name="username">Registry username.</param>
    /// <param name="password">Registry password or access token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>(true, "Login Succeeded")</c> on success or
    /// <c>(false, errorMessage)</c> on authentication failure.
    /// </returns>
    Task<(bool Success, string Message)> LoginAsync(string serverAddress, string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Signals logout from a container registry.
    /// This operation is best-effort; failures are logged but do not throw.
    /// </summary>
    /// <param name="serverAddress">Registry server address to log out from.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LogoutAsync(string serverAddress, CancellationToken ct = default);

    /// <summary>
    /// Tests container registry credentials by performing a login probe immediately
    /// followed by logout. No state is persisted and no registry entity is created.
    /// Intended for pre-validation in client forms before creating a registry entry.
    /// </summary>
    /// <param name="serverAddress">Registry server address to test.</param>
    /// <param name="username">Username for the test probe.</param>
    /// <param name="password">Password or token for the test probe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>(true, "Login successful.")</c> when credentials are valid or
    /// <c>(false, errorMessage)</c> when authentication fails.
    /// </returns>
    Task<(bool Success, string Message)> TestRegistryAsync(string serverAddress, string username, string password, CancellationToken ct = default);
}
