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
using AppOrchestrator.Api.Core.Options;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Services.Stacks;

/// <inheritdoc cref="IStackBackupService"/>
public class StackBackupService(
    IFileService fileService,
    IDockerComposeCommandRunner commandRunner,
    IComposeEnvironmentBuilder envBuilder,
    IOptions<OrchestratorOptions> options,
    ILogger<StackBackupService> logger)
    : IStackBackupService
{
    private readonly OrchestratorOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task ApplyWithBackupAsync(
        string projectName,
        Stream newComposeStream,
        Dictionary<string, string> envConfig,
        string networkName,
        Stream? packageZip = null,
        CancellationToken ct = default)
    {
        var workspacePath = fileService.GetInternalWorkspacePath(projectName);
        var backupPath = $"{workspacePath}_backup_{DateTime.UtcNow:yyyyMMddHHmmss}";

        if (fileService.DirectoryExists(workspacePath))
        {
            fileService.CopyDirectory(workspacePath, backupPath);
            logger.LogInformation("Backup created at {Backup}", backupPath);
        }

        try
        {
            fileService.CreateDirectory(workspacePath);
            await fileService.WriteComposeFileAsync(workspacePath, newComposeStream, ct);
            await fileService.WriteEnvFileAsync(workspacePath, envConfig, ct);
            await fileService.ExtractPackageFilesAsync(workspacePath, packageZip, ct);

            try
            {
                await RunComposeUpOrThrowAsync(workspacePath, projectName, "up -d (version update)", networkName, ct);
            }
            catch (DockerOperationException ex)
            {
                logger.LogError("Update failed [{Project}] - restoring backup. Error: {Stderr}", projectName, ex.Message);
                await RestoreBackupAsync(backupPath, projectName, networkName, ct);
                throw;
            }
        }
        catch (DockerOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during version update for {Project} - attempting backup restore", projectName);
            try
            {
                await RestoreBackupAsync(backupPath, projectName, networkName, ct);
            }
            catch
            {
                // Restore failure is logged inside RestoreBackupAsync; swallow here to preserve original exception.
            }

            throw;
        }

        CleanupOldBackups(workspacePath, projectName);

        logger.LogInformation("Stack {Project} updated. Backup kept at {Backup}.", projectName, backupPath);
    }

    private async Task RestoreBackupAsync(string backupPath, string projectName, string networkName, CancellationToken ct)
    {
        var workspacePath = fileService.GetInternalWorkspacePath(projectName);
        if (!fileService.DirectoryExists(backupPath))
            return;

        fileService.CopyDirectory(backupPath, workspacePath, overwrite: true);
        await RunComposeUpOrThrowAsync(workspacePath, projectName, "up -d (restore backup)", networkName, ct);
        logger.LogInformation("Backup restored for {Project}.", projectName);
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

    private void CleanupOldBackups(string workspacePath, string projectName)
    {
        var retention = Math.Max(_options.VersionUpdateBackupRetention, 0);
        if (retention == 0)
            return;

        var stacksRoot = Path.GetDirectoryName(workspacePath);
        if (string.IsNullOrWhiteSpace(stacksRoot) || !Directory.Exists(stacksRoot))
            return;

        var backups = Directory
            .GetDirectories(stacksRoot, $"{projectName}_backup_*")
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .ToList();

        foreach (var backup in backups.Skip(retention))
        {
            try
            {
                fileService.DeleteDirectory(backup);
                logger.LogInformation("Deleted old backup {Backup} (retention={Retention}).", backup, retention);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old backup {Backup}.", backup);
            }
        }
    }
}
