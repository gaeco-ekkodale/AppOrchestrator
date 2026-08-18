// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Diagnostics;

namespace AppOrchestrator.Api.Services.Docker;

/// <inheritdoc cref="IDockerRegistryService"/>
public class DockerRegistryService(
    ILogger<DockerRegistryService> logger,
    IDockerClient dockerClient)
    : IDockerRegistryService
{
    // -----------------------------------------------------------------------
    // Registry authentication
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<(bool Success, string Message)> LoginAsync(string serverAddress, string username, string password, CancellationToken ct = default)
    {
        try
        {
            await dockerClient.System.AuthenticateAsync(
                new AuthConfig
                {
                    ServerAddress = serverAddress,
                    Username = username,
                    Password = password
                },
                ct);

            await RunDockerLoginAsync(serverAddress, username, password, ct);
            logger.LogInformation("Docker login succeeded for {Registry}", serverAddress);
            return (true, "Login Succeeded");
        }
        catch (DockerApiException ex)
        {
            logger.LogWarning("Docker login failed for {Registry}: {Error}", serverAddress, ex.Message);
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during docker login for {Registry}", serverAddress);
            return (false, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(string serverAddress, CancellationToken ct = default)
    {
        await RunDockerLogoutAsync(serverAddress, ct);
        logger.LogInformation("Docker logout succeeded for {Registry}", serverAddress);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message)> TestRegistryAsync(string serverAddress, string username, string password, CancellationToken ct = default)
    {
        try
        {
            await dockerClient.System.AuthenticateAsync(
                new AuthConfig
                {
                    ServerAddress = serverAddress,
                    Username = username,
                    Password = password
                },
                ct);

            return (true, "Login successful.");
        }
        catch (DockerApiException ex)
        {
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during docker login test for {Registry}", serverAddress);
            return (false, ex.Message);
        }
    }

    // -----------------------------------------------------------------------
    // Docker CLI helpers
    // -----------------------------------------------------------------------

    private async Task RunDockerLoginAsync(string serverAddress, string username, string password, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"login {serverAddress} -u {username} --password-stdin",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        await process.StandardInput.WriteAsync(password);
        process.StandardInput.Close();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            logger.LogWarning("docker login CLI failed for {Registry}: {Error}", serverAddress, stderr);
        }
    }

    private async Task RunDockerLogoutAsync(string serverAddress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"logout {serverAddress}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            logger.LogWarning("docker logout CLI failed for {Registry}: {Error}", serverAddress, stderr);
        }
    }
}
