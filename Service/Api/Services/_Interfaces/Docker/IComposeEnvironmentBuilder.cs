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
/// Builds the complete process-environment dictionary for <c>docker compose</c> CLI invocations.
///
/// Centralises the assembly of:
/// <list type="bullet">
///   <item><description>System variables from <c>OrchestratorOptions</c> (<c>DOCKER_HOST</c>, <c>NETWORK_TRAEFIK</c>, <c>VOLUME_BASE_PATH</c>).</description></item>
///   <item><description>Per-stack variables (<c>STACK_NAME</c>, <c>NETWORK_NAME</c>).</description></item>
///   <item><description>Shared network-level variables from the database.</description></item>
/// </list>
///
/// This removes the need for every caller of <see cref="IDockerComposeCommandRunner"/> to
/// individually resolve network entities and assemble environment dictionaries.
/// </summary>
public interface IComposeEnvironmentBuilder
{
    /// <summary>
    /// Well-known environment variable keys injected into every compose process.
    /// </summary>
    static class Keys
    {
        public const string StackName = "STACK_NAME";
        public const string EnvironmentNetwork = "ENVIRONMENT_NETWORK";
        public const string TraefikNetwork = "TRAEFIK_NETWORK";
        public const string VolumeBasePath = "VOLUME_BASE_PATH";
        public const string PackageFilesDir = "PACKAGE_FILES_DIR";

        public const string DockerHost = "DOCKER_HOST";
    }

    /// <summary>
    /// Builds the full process-environment dictionary for a compose invocation.
    /// </summary>
    /// <param name="projectName">Docker Compose project name (also used as <c>STACK_NAME</c>).</param>
    /// <param name="networkName">
    /// Name of the assigned Docker network. When non-empty the builder injects <c>NETWORK_NAME</c>
    /// and resolves shared environment variables defined on the <see cref="Domain.Models.Network"/> entity.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A dictionary ready to be applied to <see cref="System.Diagnostics.ProcessStartInfo.Environment"/>.
    /// </returns>
    Task<Dictionary<string, string>> BuildAsync(string projectName, string networkName, CancellationToken ct = default);
}
