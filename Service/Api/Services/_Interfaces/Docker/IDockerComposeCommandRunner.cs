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
/// Represents the outcome of a single <c>docker compose</c> CLI invocation.
/// </summary>
/// <param name="ExitCode">
/// Process exit code. A value of <c>0</c> indicates success; any other value indicates failure.
/// </param>
/// <param name="Stdout">
/// Captured standard output of the process. May be empty for commands that write only to stderr.
/// </param>
/// <param name="Stderr">
/// Captured standard error of the process. Typically contains Docker Engine log output and
/// error messages when the exit code is non-zero.
/// </param>
public record DockerComposeCommandResult(int ExitCode, string Stdout, string Stderr);

/// <summary>
/// Executes <c>docker compose up -d --remove-orphans</c> in a controlled subprocess with
/// a caller-supplied process environment dictionary and structured per-line logging.
///
/// Centralising CLI invocation here keeps Docker compose process-spawning specifics out of
/// higher-level services and makes the command runner independently mockable in tests.
/// The environment dictionary is built by <see cref="IComposeEnvironmentBuilder"/>.
/// </summary>
public interface IDockerComposeCommandRunner
{
    /// <summary>
    /// Runs <c>docker compose up -d --remove-orphans</c> (optionally with <c>--force-recreate</c>)
    /// inside the given working directory for the specified Compose project name.
    /// </summary>
    /// <param name="workingDirectory">
    /// Absolute path to the workspace directory that contains <c>docker-compose.yml</c>
    /// and <c>.env</c>. This becomes the working directory for the spawned process so that
    /// Docker Compose can locate its configuration files.
    /// </param>
    /// <param name="projectName">
    /// Docker Compose project name passed via <c>-p &lt;projectName&gt;</c>.
    /// </param>
    /// <param name="environment">
    /// Complete set of extra process environment variables to inject into the compose process.
    /// Built by <see cref="IComposeEnvironmentBuilder.BuildAsync"/> which includes system,
    /// per-stack, and shared network-level variables.
    /// </param>
    /// <param name="forceRecreate">
    /// When <c>true</c>, appends <c>--force-recreate</c> to the command so all containers
    /// are recreated even if their configuration has not changed. Use for restart scenarios.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="DockerComposeCommandResult"/> with the exit code and captured output streams.
    /// The caller is responsible for inspecting <see cref="DockerComposeCommandResult.ExitCode"/>
    /// and reacting to non-zero values.
    /// </returns>
    Task<DockerComposeCommandResult> RunComposeUpAsync(
        string workingDirectory,
        string projectName,
        Dictionary<string, string> environment,
        bool forceRecreate = false,
        CancellationToken ct = default);
}
