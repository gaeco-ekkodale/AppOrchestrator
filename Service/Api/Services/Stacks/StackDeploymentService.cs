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
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.Utils;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;

namespace AppOrchestrator.Api.Services.Stacks;

/// <inheritdoc cref="IStackDeploymentService"/>
public class StackDeploymentService(
    IStackRepository stackRepo,
    IAppRegistryRepository appRegistryRepo,
    IDockerProjectService dockerProjectService,
    IAppRegistryClient appRegistryClient,
    IFileService fileService,
    IDockerComposeCommandRunner commandRunner,
    IComposeEnvironmentBuilder envBuilder,
    IStackBackupService backupService,
    ILogger<StackDeploymentService> logger)
    : IStackDeploymentService
{

    /// <inheritdoc/>
    public async Task<Stack> CreateFromRegistryAsync(CreateStackFromRegistryCommand command, CancellationToken ct = default)
    {
        var registry = await appRegistryRepo.GetByIdAsync(command.RegistryId, ct)
            ?? throw new KeyNotFoundException($"Registry '{command.RegistryId}' was not found.");

        var projectName = ProjectName.FromStackName(command.StackName, command.NetworkName);
        var existing = await stackRepo.GetAsync(projectName, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A stack named '{command.StackName}' already exists.");

        await using var composeStream = await appRegistryClient.FetchComposeFileAsync(
            registry,
            command.PackageId,
            command.Version,
            ct);

        await using var zipStream = await appRegistryClient.DownloadPackageZipAsync(
            registry,
            command.PackageId,
            command.Version,
            ct);

        await DeployAsync(projectName, composeStream, zipStream, command.EnvConfig, command.NetworkName, ct);

        var stack = new RegistryStack
        {
            Id = Guid.NewGuid(),
            StackName = command.StackName,
            DockerProjectName = projectName,
            AppRegistryId = registry.Id,
            PackageId = command.PackageId,
            PackageVersion = command.Version,
            NetworkName = command.NetworkName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await stackRepo.AddAsync(stack, ct);
        return (await stackRepo.GetAsync(projectName, ct))!;
    }

    /// <inheritdoc/>
    public async Task<Stack> CreateCustomAsync(CreateCustomStackCommand command, CancellationToken ct = default)
    {
        var projectName = ProjectName.FromStackName(command.StackName, command.NetworkName);
        var existing = await stackRepo.GetAsync(projectName, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A stack named '{command.StackName}' already exists.");

        // Custom stacks ship no package files.
        await using var composeStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(command.ComposeContent));
        await DeployAsync(projectName, composeStream, packageZip: null, command.EnvConfig, command.NetworkName, ct);

        var stack = new CustomStack
        {
            Id = Guid.NewGuid(),
            StackName = command.StackName,
            DockerProjectName = projectName,
            NetworkName = command.NetworkName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await stackRepo.AddAsync(stack, ct);
        return (await stackRepo.GetAsync(projectName, ct))!;
    }

    /// <inheritdoc/>
    public async Task<Stack> CloneAsync(CloneStackCommand command, CancellationToken ct = default)
    {
        var source = await stackRepo.GetAsync(command.SourceProjectName, ct)
            ?? throw new KeyNotFoundException($"Source stack '{command.SourceProjectName}' was not found.");

        var cloneStackName = command.StackName ?? source.StackName;
        var cloneNetworkName = command.NetworkName ?? source.NetworkName ?? "";

        // The project name is derived from both name and network, so a clone is only distinguishable
        // when at least one of them changes.
        var newProjectName = ProjectName.FromStackName(cloneStackName, cloneNetworkName);
        if (string.Equals(newProjectName, source.DockerProjectName, StringComparison.Ordinal))
            throw new ArgumentException(
                "A clone needs either a different stack name or a different network.", nameof(command));

        var conflict = await stackRepo.GetAsync(newProjectName, ct);
        if (conflict is not null)
            throw new InvalidOperationException($"A stack named '{cloneStackName}' already exists on network '{cloneNetworkName}'.");

        var newWorkspacePath = fileService.GetInternalWorkspacePath(newProjectName);
        var sourceWorkspacePath = fileService.GetInternalWorkspacePath(source.DockerProjectName);

        if (fileService.DirectoryExists(sourceWorkspacePath))
        {
            // Compose, .env, package files and volume data - the clone starts from identical state.
            fileService.CopyDirectory(sourceWorkspacePath, newWorkspacePath, overwrite: true);
            logger.LogInformation("Copied workspace from {Source} to {Target}", sourceWorkspacePath, newWorkspacePath);
        }
        else
        {
            logger.LogWarning("Source workspace {Path} not found - creating empty workspace for clone.", sourceWorkspacePath);
            fileService.CreateDirectory(newWorkspacePath);
        }

        Stack clone = source switch
        {
            RegistryStack rs => new RegistryStack
            {
                Id = Guid.NewGuid(),
                StackName = cloneStackName,
                DockerProjectName = newProjectName,
                AppRegistryId = rs.AppRegistryId,
                PackageId = rs.PackageId,
                PackageVersion = rs.PackageVersion,
                NetworkName = cloneNetworkName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            _ => new CustomStack
            {
                Id = Guid.NewGuid(),
                StackName = cloneStackName,
                DockerProjectName = newProjectName,
                NetworkName = cloneNetworkName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await stackRepo.AddAsync(clone, ct);

        return (await stackRepo.GetAsync(newProjectName, ct))!;
    }

    /// <inheritdoc/>
    public async Task<Stack> UpdateAsync(UpdateStackCommand command, CancellationToken ct = default)
    {
        if (command.StackName is null && command.Version is null && command.EnvConfig is null && command.NetworkName is null)
            throw new ArgumentException("Provide at least one of StackName, Version, EnvConfig or NetworkName.", nameof(command));

        var stack = await stackRepo.GetAsync(command.ProjectName, ct)
            ?? throw new KeyNotFoundException($"Stack '{command.ProjectName}' was not found.");

        // Compute effective values early so the rename check and all compose calls are consistent.
        var effectiveStackName = command.StackName ?? stack.StackName;
        var effectiveNetworkName = command.NetworkName switch
        {
            null => stack.NetworkName ?? "",
            "" => "",
            _ => command.NetworkName
        };

        // Rename whenever the derived project name changes — either StackName or NetworkName
        // (both are embedded in the project name) may have caused the change.
        var newProjectName = ProjectName.FromStackName(effectiveStackName, effectiveNetworkName);
        if (!string.Equals(newProjectName, stack.DockerProjectName, StringComparison.Ordinal))
        {
            var conflict = await stackRepo.GetAsync(newProjectName, ct);
            if (conflict is not null)
                throw new InvalidOperationException($"A stack named '{effectiveStackName}' already exists.");

            var liveStatus = await dockerProjectService.GetProjectStatusAsync(stack.DockerProjectName, ct);
            if (liveStatus == Shared.DTOs.StackStatus.Running)
                await dockerProjectService.RemoveProjectAsync(stack.DockerProjectName, ct);

            var newAbsPath = fileService.GetInternalWorkspacePath(newProjectName);
            var oldAbsPath = fileService.GetInternalWorkspacePath(stack.DockerProjectName);
            if (fileService.DirectoryExists(oldAbsPath))
                fileService.MoveDirectory(oldAbsPath, newAbsPath);
            else
                fileService.CreateDirectory(newAbsPath);

            stack.StackName = effectiveStackName;
            stack.DockerProjectName = newProjectName;
        }

        if (command.Version is not null || command.EnvConfig is not null)
        {
            var envConfig = command.EnvConfig
                ?? await fileService.ReadEnvFileAsync(fileService.GetInternalWorkspacePath(stack.DockerProjectName), ct);

            if (command.Version is not null)
            {
                if (stack is not RegistryStack registryStack)
                    throw new ArgumentException("This is a custom stack with no linked registry - version update is not supported.", nameof(command));

                var registry = await appRegistryRepo.GetByIdAsync(registryStack.AppRegistryId, ct)
                    ?? throw new KeyNotFoundException($"Source registry (id={registryStack.AppRegistryId}) was not found.");

                await using var composeStream = await appRegistryClient.FetchComposeFileAsync(
                    registry,
                    registryStack.PackageId,
                    command.Version,
                    ct);

                // Each version ships its own set of mounted package files.
                await using var zipStream = await appRegistryClient.DownloadPackageZipAsync(
                    registry,
                    registryStack.PackageId,
                    command.Version,
                    ct);

                await backupService.ApplyWithBackupAsync(
                    stack.DockerProjectName, composeStream, envConfig, effectiveNetworkName, zipStream, ct);
                registryStack.PackageVersion = command.Version;
            }
            else
            {
                await UpdateEnvAsync(stack.DockerProjectName, envConfig, effectiveNetworkName, ct);
            }
        }
        else if (command.NetworkName is not null)
        {
            // Network (and possibly also the project name) changed without a version/env update.
            // Re-run compose so containers are moved to the new network.
            var envConfig = await fileService.ReadEnvFileAsync(fileService.GetInternalWorkspacePath(stack.DockerProjectName), ct);
            await UpdateEnvAsync(stack.DockerProjectName, envConfig, effectiveNetworkName, ct);
        }

        stack.UpdatedAt = DateTime.UtcNow;

        if (command.NetworkName is not null)
            stack.NetworkName = command.NetworkName;

        await stackRepo.UpdateAsync(stack, ct);

        return (await stackRepo.GetAsync(stack.DockerProjectName, ct))!;
    }

    /// <inheritdoc/>
    public async Task<StackComposeData> UpdateComposeAsync(UpdateStackComposeCommand command, CancellationToken ct = default)
    {
        var stack = await stackRepo.GetAsync(command.ProjectName, ct)
            ?? throw new KeyNotFoundException($"Stack '{command.ProjectName}' was not found.");

        if (stack is RegistryStack)
            throw new ArgumentException("This stack is linked to a registry. Use stack version updates instead of direct compose editing.", nameof(command));

        await using var composeStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(command.ComposeContent));
        await backupService.ApplyWithBackupAsync(
            stack.DockerProjectName, composeStream, command.EnvConfig, stack.NetworkName ?? "", packageZip: null, ct);

        stack.UpdatedAt = DateTime.UtcNow;
        await stackRepo.UpdateAsync(stack, ct);

        var workspacePath = fileService.GetInternalWorkspacePath(stack.DockerProjectName);
        return new StackComposeData(
            stack.StackName,
            await fileService.ReadComposeFileAsync(workspacePath, ct),
            await fileService.ReadEnvFileAsync(workspacePath, ct));
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string projectName, CancellationToken ct = default)
    {
        await dockerProjectService.RemoveProjectAsync(projectName, ct);
        var workspacePath = fileService.GetInternalWorkspacePath(projectName);
        if (fileService.DirectoryExists(workspacePath))
            fileService.DeleteDirectory(workspacePath);
        await stackRepo.DeleteAsync(projectName, ct);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes workspace files and runs <c>docker compose up -d</c>.
    /// </summary>
    private async Task DeployAsync(
        string projectName,
        Stream composeStream,
        Stream? packageZip,
        Dictionary<string, string> envConfig,
        string networkName,
        CancellationToken ct)
    {
        var workspacePath = fileService.GetInternalWorkspacePath(projectName);
        fileService.CreateDirectory(workspacePath);
        await fileService.WriteComposeFileAsync(workspacePath, composeStream, ct);
        await fileService.WriteEnvFileAsync(workspacePath, envConfig, ct);
        await fileService.ExtractPackageFilesAsync(workspacePath, packageZip, ct);

        await RunComposeUpOrThrowAsync(workspacePath, projectName, "up -d", networkName, ct);

        logger.LogInformation("Stack {Project} deployed.", projectName);
    }

    private async Task UpdateEnvAsync(
        string projectName,
        Dictionary<string, string> envConfig,
        string networkName,
        CancellationToken ct)
    {
        var workspacePath = fileService.GetInternalWorkspacePath(projectName);
        await fileService.WriteEnvFileAsync(workspacePath, envConfig, ct);

        await RunComposeUpOrThrowAsync(workspacePath, projectName, "up -d (env update)", networkName, ct);
    }

    private async Task RunComposeUpOrThrowAsync(
        string workspacePath,
        string projectName,
        string operation,
        string networkName,
        CancellationToken ct)
    {
        var env = await envBuilder.BuildAsync(projectName, networkName, ct);
        var result = await commandRunner.RunComposeUpAsync(
            workspacePath,
            projectName,
            env,
            ct: ct);

        if (result.ExitCode == 0)
            return;

        logger.LogError("docker compose {Operation} failed [{Project}]: {Stderr}",
            operation, projectName, result.Stderr);

        throw new DockerOperationException(projectName, operation, result.Stderr.TrimEnd());
    }
}
