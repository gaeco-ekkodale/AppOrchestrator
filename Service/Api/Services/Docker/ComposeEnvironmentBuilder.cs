// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Options;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Services.Docker;

/// <inheritdoc cref="IComposeEnvironmentBuilder"/>
public class ComposeEnvironmentBuilder(
    IOptions<OrchestratorOptions> options,
    INetworkRepository networkRepo,
    IFileService fileService)
    : IComposeEnvironmentBuilder
{
    private readonly OrchestratorOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> BuildAsync(
        string projectName,
        string networkName,
        CancellationToken ct = default)
    {
        // Resolve the host-side workspace path so the Docker daemon can create bind-mount
        // volumes at the correct host location. Backslashes are normalised to forward slashes
        // so Docker Compose V2 on Linux recognises the Windows drive prefix (e.g. C:/...) as
        // an absolute path and does not prepend the project directory.
        var hostWorkspacePath = await fileService.GetHostWorkspacePath(projectName, ct);
        var volumeBasePath = hostWorkspacePath.Replace('\\', '/');

        var env = new Dictionary<string, string>
        {
            [IComposeEnvironmentBuilder.Keys.StackName] = projectName,
            [IComposeEnvironmentBuilder.Keys.TraefikNetwork] = _options.TraefikNetwork,
            [IComposeEnvironmentBuilder.Keys.DockerHost] = NormaliseDockerHost(_options.DockerHostUri),
            [IComposeEnvironmentBuilder.Keys.VolumeBasePath] = volumeBasePath + "/volumes",
            [IComposeEnvironmentBuilder.Keys.PackageFilesDir] = volumeBasePath + "/package_files"
        };

        if (!string.IsNullOrEmpty(networkName))
        {
            env[IComposeEnvironmentBuilder.Keys.EnvironmentNetwork] = networkName;

            // Resolve shared environment variables defined on the network entity.
            var network = await networkRepo.GetByNameAsync(networkName, ct);
            if (network is not null)
            {
                foreach (var variable in network.EnvironmentVariables)
                    env[variable.Name] = variable.Value;
            }
        }

        return env;
    }

    /// <summary>
    /// Normalises the Docker host URI for Windows named pipes.
    /// Docker CLI expects <c>npipe:////./pipe/…</c> with four slashes.
    /// </summary>
    private static string NormaliseDockerHost(string dockerHost)
    {
        if (dockerHost.StartsWith("npipe://", StringComparison.OrdinalIgnoreCase) &&
            !dockerHost.StartsWith("npipe:////", StringComparison.OrdinalIgnoreCase))
            return dockerHost.Replace("npipe://", "npipe:////");

        return dockerHost;
    }
}
