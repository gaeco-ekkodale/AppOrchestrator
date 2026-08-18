// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Shared.DTOs;

namespace AppOrchestrator.Api.Services._Interfaces.Docker;

/// <summary>
/// Provides Docker container-level operations within a compose project.
///
/// This interface encapsulates listing, inspecting, reading logs, and
/// controlling the lifecycle of individual containers. All Docker-specific
/// error handling and filter construction is encapsulated here so that
/// callers (endpoints) remain free of direct Docker SDK dependencies.
/// </summary>
public interface IDockerContainerService
{
    /// <summary>
    /// Returns all containers (including stopped ones) that belong to the compose
    /// project, mapped to <see cref="ContainerDTO"/>.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ContainerDTO>> ListContainersAsync(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Finds a single container within the compose project by matching short or
    /// full container ID, or container name (leading slash is ignored).
    /// Returns <c>null</c> when no matching container is found.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="containerId">Short/full container ID or container name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ContainerDTO?> GetContainerAsync(string projectName, string containerId, CancellationToken ct = default);

    /// <summary>
    /// Fetches log lines for a specific container inside the compose project with
    /// cursor-based incremental polling support.
    /// Pass the <c>NextSince</c> value from a previous response as <paramref name="since"/>
    /// to receive only lines that were emitted after that timestamp.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="containerId">Short/full container ID or container name.</param>
    /// <param name="since">
    /// ISO 8601 timestamp cursor. Only lines after this time are returned.
    /// Pass <c>null</c> to start from the tail.
    /// </param>
    /// <param name="tail">
    /// Maximum number of historical lines to fetch per request.
    /// A value of 0 uses the service default.
    /// </param>
    /// <param name="limit">
    /// Maximum number of lines to include in the response after filtering.
    /// A value of 0 uses the service default.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<ContainerLogsResponseDTO> GetContainerLogsAsync(
        string projectName,
        string containerId,
        string? since,
        int tail,
        int limit,
        CancellationToken ct = default);

    /// <summary>
    /// Starts a single stopped container that belongs to the compose project.
    /// The container is identified by short/full ID or container name.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="containerId">Short/full container ID or container name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the container is not found in the project.
    /// </exception>
    Task StartContainerAsync(string projectName, string containerId, CancellationToken ct = default);

    /// <summary>
    /// Stops a single running container that belongs to the compose project.
    /// The container is identified by short/full ID or container name.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="containerId">Short/full container ID or container name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the container is not found in the project.
    /// </exception>
    Task StopContainerAsync(string projectName, string containerId, CancellationToken ct = default);

    /// <summary>
    /// Stops then immediately starts a single container that belongs to the compose
    /// project. Equivalent to <see cref="StopContainerAsync"/> followed by
    /// <see cref="StartContainerAsync"/>.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="containerId">Short/full container ID or container name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the container is not found in the project.
    /// </exception>
    Task RestartContainerAsync(string projectName, string containerId, CancellationToken ct = default);
}
