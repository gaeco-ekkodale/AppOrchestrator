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
/// Provides Docker compose project lifecycle operations.
///
/// This interface encapsulates all project-level Docker operations:
/// starting, stopping, restarting, and removing compose projects,
/// as well as querying their runtime status and discovering active projects.
/// All Docker-specific error handling and filter construction is encapsulated
/// here so that callers (endpoints, deployment services) remain free of
/// direct Docker SDK dependencies.
/// </summary>
public interface IDockerProjectService
{
    /// <summary>
    /// Starts all stopped containers that belong to the given compose project.
    /// For orchestrator-managed stacks (those with a persisted workspace directory)
    /// a full <c>docker compose up -d</c> is executed so services start in
    /// <c>depends_on</c> order. For external stacks all containers are started
    /// individually via the Docker API.
    /// </summary>
    /// <param name="projectName">Docker Compose project name (orch-* prefix).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="Core.Exceptions.DockerOperationException">
    /// Thrown when the Docker API or CLI reports an error.
    /// </exception>
    Task StartProjectAsync(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Stops all running containers in the compose project without removing them.
    /// Containers that are already stopped are silently skipped.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="Core.Exceptions.DockerOperationException">
    /// Thrown when the Docker API returns an error while stopping containers.
    /// </exception>
    Task StopProjectAsync(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Restarts all containers in the compose project.
    /// For orchestrator-managed stacks (those with a persisted workspace directory)
    /// <c>docker compose up -d</c> is used so services restart in
    /// <c>depends_on</c> order. For external stacks all containers are restarted
    /// via the Docker API.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no containers are found for an external project.
    /// </exception>
    /// <exception cref="Core.Exceptions.DockerOperationException">
    /// Thrown when the Docker API returns an error during restart.
    /// </exception>
    Task RestartProjectAsync(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Stops and permanently removes all containers and networks that belong to
    /// the compose project. Volumes and the workspace directory are not touched.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="Core.Exceptions.DockerOperationException">
    /// Thrown when the Docker API reports an error during container removal.
    /// </exception>
    Task RemoveProjectAsync(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Queries the Docker Engine API and returns the aggregate runtime status of
    /// the compose project based on the state of all its containers.
    /// Returns <see cref="StackStatus.Unknown"/> when an error occurs, or when
    /// no containers belong to the project.
    /// </summary>
    /// <param name="projectName">Docker Compose project name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="StackStatus.Running"/> — all containers are running.<br/>
    /// <see cref="StackStatus.Partial"/> — some containers are running.<br/>
    /// <see cref="StackStatus.Stopped"/> — all containers are stopped.<br/>
    /// <see cref="StackStatus.Unknown"/> — no containers found or Docker unreachable.
    /// </returns>
    Task<StackStatus> GetProjectStatusAsync(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Returns the full set of Docker Compose project names that currently have
    /// at least one container (running or stopped) tracked by the local Docker daemon.
    /// Used to discover stacks that exist in Docker but are not persisted in the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> of project names using ordinal comparison.
    /// </returns>
    Task<HashSet<string>> ListComposeProjectNamesAsync(CancellationToken ct = default);
}
