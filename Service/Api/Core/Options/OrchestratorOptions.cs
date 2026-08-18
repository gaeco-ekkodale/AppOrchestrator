// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Core.Options;

/// <summary>
/// Configuration for the Orchestrator runtime behaviour.
/// </summary>
public class OrchestratorOptions
{
    public const string SectionName = "Orchestrator";

    /// <summary>
    /// Root directory of the orchestrator volume mount.
    /// In production this matches the container-side of the Docker volume bind-mount,
    /// e.g. ./volumes/orchestrator:/orchestrator  ->  /orchestrator
    /// For local development without Docker set this to the host-side path,
    /// e.g. ./volumes/orchestrator (relative to the project root).
    /// </summary>
    public string RootPath { get; set; } = "/orchestrator";

    /// <summary>
    /// Name of the external Traefik Docker network that is injected as
    /// TRAEFIK_NETWORK into every stack's .env file.
    /// </summary>
    public string TraefikNetwork { get; set; } = string.Empty;

    /// <summary>
    /// Full URI for the Docker daemon endpoint.
    /// Used both by the Docker.DotNet API client and as DOCKER_HOST for CLI sub-processes.
    ///
    /// Linux / Docker container (default):
    ///   unix:///var/run/docker.sock
    ///   -> matches the bind-mount: /var/run/docker.sock:/var/run/docker.sock
    ///
    /// Windows - local development with Docker Desktop (Visual Studio):
    ///   npipe://./pipe/docker_engine
    /// </summary>
    public string DockerHostUri { get; set; } = "unix:///var/run/docker.sock";

    /// <summary>
    /// Maximum number of version-update backup folders to keep per stack.
    /// Older backups are removed after successful updates.
    /// Set to 0 to disable cleanup.
    /// </summary>
    public int VersionUpdateBackupRetention { get; set; } = 5;
}
