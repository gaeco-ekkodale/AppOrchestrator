// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces.Stacks;

/// <summary>
/// Handles backup creation, atomic application, and rollback of stack workspace files
/// during version update operations.
///
/// Before writing new files the service snapshots the current workspace; if the
/// subsequent <c>docker compose up -d</c> fails the snapshot is restored automatically.
/// Old snapshots exceeding the configured retention limit are pruned after a successful update.
/// </summary>
public interface IStackBackupService
{
    /// <summary>
    /// Writes <paramref name="newComposeStream"/> and <paramref name="envConfig"/> to the
    /// stack workspace, then re-deploys via <c>docker compose up -d</c>.
    ///
    /// If the deploy step fails the previous workspace state is restored from the backup
    /// and the compose command is re-run so the stack returns to its last known-good state.
    /// Old backups exceeding the configured retention count are deleted on success.
    /// </summary>
    /// <param name="projectName">Docker Compose project name identifying the target workspace.</param>
    /// <param name="newComposeStream">Stream containing the new <c>docker-compose.yml</c> content. Disposed by the caller.</param>
    /// <param name="envConfig">Key-value environment variables to write to the workspace <c>.env</c> file.</param>
    /// <param name="networkName">Name of the network the stack is deployed to; empty when detached.</param>
    /// <param name="packageZip">
    /// Package ZIP whose files replace the workspace <c>package_files/</c> directory.
    /// Pass <c>null</c> to keep the current package files (custom stacks).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="AppOrchestrator.Api.Core.Exceptions.DockerOperationException">
    /// Thrown when <c>docker compose up -d</c> fails even after a backup restore attempt.
    /// </exception>
    Task ApplyWithBackupAsync(
        string projectName,
        Stream newComposeStream,
        Dictionary<string, string> envConfig,
        string networkName,
        Stream? packageZip = null,
        CancellationToken ct = default);
}
