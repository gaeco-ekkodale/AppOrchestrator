// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces.Mfe;

/// <summary>
/// Sends MFE plugin snapshots to the plugin host running in a given Docker network.
///
/// Every state change — plugin start, stop, deploy, delete, or host start — triggers a full
/// snapshot of all MFE containers in the affected network so the host can reconcile its state.
/// All calls are best-effort: HTTP failures are logged and do not propagate to callers.
/// </summary>
public interface IMfeSyncService
{
    /// <summary>
    /// Builds a snapshot of all MFE containers (running <em>and</em> stopped) in the Docker
    /// network and sends it to a host discovered from container labels.
    ///
    /// Host discovery in the same network:
    /// <list type="bullet">
    ///   <item>Container label <c>orchestrator.host=true</c></item>
    ///   <item>Container label <c>orchestrator.apiKey=&lt;key&gt;</c></item>
    /// </list>
    /// </summary>
    /// <param name="networkName">Docker network name to scan for host and plugins.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SyncNetworkAsync(string networkName, CancellationToken ct = default);

    /// <summary>
    /// Syncs the network after a stack deployment. Checks whether the deployed stack
    /// (identified by its Docker Compose project name) contains MFE plugin containers.
    /// <list type="bullet">
    ///   <item>
    ///     <b>Stack has plugins:</b> Sends a strict snapshot with up to 5 retry attempts
    ///     (2 s delay) and throws <see cref="HttpRequestException"/> on final failure so
    ///     the caller can roll back.
    ///   </item>
    ///   <item>
    ///     <b>Stack has no plugins:</b> Sends a best-effort snapshot; errors are logged
    ///     but never propagated.
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="networkName">Docker network name to scan for host and plugins.</param>
    /// <param name="dockerProjectName">
    /// Docker Compose project name (<c>com.docker.compose.project</c> label) of the
    /// freshly deployed stack, used to determine whether the stack contains plugins.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task SyncAfterDeployAsync(string networkName, string dockerProjectName, CancellationToken ct = default);
}


