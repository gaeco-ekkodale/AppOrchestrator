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
using System.Diagnostics;
using System.Text;

namespace AppOrchestrator.Api.Services.Docker;

/// <summary>
/// Shared executor for docker compose CLI commands.
/// Applies a caller-supplied environment dictionary built by <see cref="IComposeEnvironmentBuilder"/>.
/// </summary>
/// <inheritdoc cref="IDockerComposeCommandRunner"/>
public class DockerComposeCommandRunner(
    ILogger<DockerComposeCommandRunner> logger)
    : IDockerComposeCommandRunner
{

    /// <inheritdoc/>
    public async Task<DockerComposeCommandResult> RunComposeUpAsync(
        string workingDirectory,
        string projectName,
        Dictionary<string, string> environment,
        bool forceRecreate = false,
        CancellationToken ct = default)
    {
        // WorkingDirectory (container-internal path) is intentionally used as the compose
        // project directory so the CLI can locate docker-compose.yml and .env.
        // The correct host-side path for volume bind-mounts is provided through
        // VOLUME_BASE_PATH in the process environment (built by IComposeEnvironmentBuilder).
        var args = forceRecreate
            ? $"compose -p {projectName} up -d --remove-orphans --force-recreate"
            : $"compose -p {projectName} up -d --remove-orphans";

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Apply the complete environment dictionary built by IComposeEnvironmentBuilder.
        foreach (var (key, value) in environment)
            psi.Environment[key] = value;

        var dockerHost = environment.GetValueOrDefault(IComposeEnvironmentBuilder.Keys.DockerHost, "");

        logger.LogInformation("docker {Args} [{Project}] (DOCKER_HOST={Host})",
            args, projectName, dockerHost);

        using var process = new Process { StartInfo = psi };
        var stdoutSb = new StringBuilder();
        var stderrSb = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            stdoutSb.AppendLine(e.Data);
            logger.LogDebug("[{Project}] stdout: {Line}", projectName, e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            stderrSb.AppendLine(e.Data);
            logger.LogDebug("[{Project}] stderr: {Line}", projectName, e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);
        process.WaitForExit();

        logger.LogInformation("docker {Args} [{Project}] exited with code {Code}",
            args, projectName, process.ExitCode);

        return new DockerComposeCommandResult(process.ExitCode, stdoutSb.ToString(), stderrSb.ToString());
    }
}
