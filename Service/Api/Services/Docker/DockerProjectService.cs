// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Exceptions;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Domain.Repositories;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace AppOrchestrator.Api.Services.Docker;

/// <inheritdoc cref="IDockerProjectService"/>
public class DockerProjectService(
    ILogger<DockerProjectService> logger,
    IDockerClient dockerClient,
    IDockerComposeCommandRunner commandRunner,
    IComposeEnvironmentBuilder envBuilder,
    IStackRepository stackRepository,
    IFileService fileService)
    : IDockerProjectService
{
    // -----------------------------------------------------------------------
    // Project-level operations
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task StartProjectAsync(string projectName, CancellationToken ct = default)
    {

        var stack = await stackRepository.GetAsync(projectName, ct);

        if (stack is not null)
        {
            var workspacePath = fileService.GetInternalWorkspacePath(projectName);
            var env = await envBuilder.BuildAsync(projectName, stack.NetworkName ?? "", ct);
            // Use compose up so services start in depends_on order.
            var result = await commandRunner.RunComposeUpAsync(
                workspacePath,
                projectName,
                env,
                ct: ct);

            if (result.ExitCode != 0)
                throw new DockerOperationException(projectName, "up -d (start)", result.Stderr.TrimEnd());

            return;
        }

        // External stack — start all stopped containers via Docker API.
        var containers = await GetProjectContainersAsync(projectName, all: true, ct);

        if (containers.Count == 0)
            return;

        var stopped = containers
            .Where(c => !c.State.Equals("running", StringComparison.OrdinalIgnoreCase))
            .ToList();

        try
        {
            await Task.WhenAll(stopped.Select(c =>
                dockerClient.Containers.StartContainerAsync(c.ID, new ContainerStartParameters(), ct)));
        }
        catch (DockerApiException ex)
        {
            logger.LogError(ex, "Docker API error starting [{Project}]", projectName);
            throw new DockerOperationException(projectName, "start containers", ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task StopProjectAsync(string projectName, CancellationToken ct = default)
    {
        var containers = await GetProjectContainersAsync(projectName, all: false, ct);
        if (containers.Count == 0)
            return;

        try
        {
            await Task.WhenAll(containers.Select(c =>
                dockerClient.Containers.StopContainerAsync(
                    c.ID,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                    ct)));
        }
        catch (DockerApiException ex)
        {
            logger.LogError(ex, "Docker API error stopping [{Project}]", projectName);
            throw new DockerOperationException(projectName, "stop containers", ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task RestartProjectAsync(string projectName, CancellationToken ct = default)
    {

        var stack = await stackRepository.GetAsync(projectName, ct);

        if (stack is not null)
        {
            var workspacePath = fileService.GetInternalWorkspacePath(projectName);
            var env = await envBuilder.BuildAsync(projectName, stack.NetworkName ?? "", ct);
            // Use compose up so services restart in depends_on order.
            var result = await commandRunner.RunComposeUpAsync(
                workspacePath,
                projectName,
                env,
                forceRecreate: true,
                ct: ct);

            if (result.ExitCode != 0)
                throw new DockerOperationException(projectName, "up -d --force-recreate (restart)", result.Stderr.TrimEnd());

            return;
        }

        // External stack — restart all containers via Docker API.
        var containers = await GetProjectContainersAsync(projectName, all: true, ct);

        if (containers.Count == 0)
            throw new InvalidOperationException($"No containers found for stack '{projectName}'.");

        try
        {
            await Task.WhenAll(containers.Select(c =>
                dockerClient.Containers.RestartContainerAsync(
                    c.ID,
                    new ContainerRestartParameters { WaitBeforeKillSeconds = 10 },
                    ct)));
        }
        catch (DockerApiException ex)
        {
            logger.LogError(ex, "Docker API error restarting [{Project}]", projectName);
            throw new DockerOperationException(projectName, "restart containers", ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveProjectAsync(string projectName, CancellationToken ct = default)
    {
        var containers = await GetProjectContainersAsync(projectName, all: true, ct);

        try
        {
            foreach (var container in containers)
            {
                if (container.State.Equals("running", StringComparison.OrdinalIgnoreCase))
                {
                    await dockerClient.Containers.StopContainerAsync(
                        container.ID,
                        new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                        ct);
                }

                await dockerClient.Containers.RemoveContainerAsync(
                    container.ID,
                    new ContainerRemoveParameters { RemoveVolumes = false, Force = false },
                    ct);
            }
        }
        catch (DockerApiException ex)
        {
            logger.LogError(ex, "Docker API error during container removal [{Project}]", projectName);
            throw new DockerOperationException(projectName, "stop/remove containers", ex.Message);
        }

        var networks = await GetProjectNetworksAsync(projectName, ct);
        foreach (var network in networks)
        {
            try
            {
                await dockerClient.Networks.DeleteNetworkAsync(network.ID, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not remove network {Id}: {Error}", network.ID, ex.Message);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<StackStatus> GetProjectStatusAsync(string projectName, CancellationToken ct = default)
    {
        try
        {
            var allContainers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
                    Filters = ProjectFilter(projectName)
                },
                ct);

            if (allContainers.Count == 0)
                return StackStatus.Unknown;

            var runningCount = allContainers.Count(c =>
                c.State.Equals("running", StringComparison.OrdinalIgnoreCase));

            if (runningCount == allContainers.Count)
                return StackStatus.Running;
            if (runningCount > 0)
                return StackStatus.Partial;

            return StackStatus.Stopped;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not query Docker status for project {Project}", projectName);
            return StackStatus.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<HashSet<string>> ListComposeProjectNamesAsync(CancellationToken ct = default)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            ct);

        return containers
            .Select(TryGetComposeProject)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private Task<IList<ContainerListResponse>> GetProjectContainersAsync(
        string projectName,
        bool all,
        CancellationToken ct) =>
        dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = all,
                Filters = ProjectFilter(projectName)
            },
            ct);

    private Task<IList<NetworkResponse>> GetProjectNetworksAsync(
        string projectName,
        CancellationToken ct) =>
        dockerClient.Networks.ListNetworksAsync(
            new NetworksListParameters
            {
                Filters = ProjectFilter(projectName)
            },
            ct);

    private const string ComposeProjectLabel = "com.docker.compose.project";

    private static Dictionary<string, IDictionary<string, bool>> ProjectFilter(string projectName) =>
        new()
        {
            ["label"] = new Dictionary<string, bool>
            {
                [$"{ComposeProjectLabel}={projectName}"] = true
            }
        };

    private static string? TryGetComposeProject(ContainerListResponse container) =>
        container.Labels.TryGetValue(ComposeProjectLabel, out var projectName)
            ? projectName
            : null;
}
