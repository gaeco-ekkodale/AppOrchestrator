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
using Docker.DotNet;
using Docker.DotNet.Models;

namespace AppOrchestrator.Api.Services.Docker;

/// <inheritdoc cref="IDockerNetworkService"/>
public class DockerNetworkService(
    ILogger<DockerNetworkService> logger,
    IDockerClient dockerClient)
    : IDockerNetworkService
{
    /// <inheritdoc/>
    public async Task CreateNetworkAsync(string networkName, CancellationToken ct = default)
    {
        try
        {
            await dockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters
                {
                    Name = networkName,
                    Driver = "bridge",
                    CheckDuplicate = true
                },
                ct);

            logger.LogInformation("Created Docker network {Name}", networkName);
        }
        catch (DockerApiException ex)
        {
            logger.LogError(ex, "Docker API error creating network {Name}", networkName);
            throw new DockerOperationException(networkName, "create network", ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteNetworkAsync(string networkName, CancellationToken ct = default)
    {
        try
        {
            await dockerClient.Networks.DeleteNetworkAsync(networkName, ct);
            logger.LogInformation("Deleted Docker network {Name}", networkName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Docker network {Name} not found during deletion \u2013 skipping", networkName);
        }
        catch (DockerApiException ex)
        {
            logger.LogError(ex, "Docker API error deleting network {Name}", networkName);
            throw new DockerOperationException(networkName, "delete network", ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasContainersAsync(string networkName, CancellationToken ct = default)
    {
        try
        {
            var network = await dockerClient.Networks.InspectNetworkAsync(networkName, ct);
            return network.Containers is { Count: > 0 };
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string networkName, CancellationToken ct = default)
    {
        try
        {
            await dockerClient.Networks.InspectNetworkAsync(networkName, ct);
            return true;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
