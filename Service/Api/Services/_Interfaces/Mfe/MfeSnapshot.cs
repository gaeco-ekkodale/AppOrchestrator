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

/// <summary>Docker container lifecycle state as represented in an <see cref="MfePayload"/>.</summary>
public enum MfePluginState
{
    Running,
    Stopped
}

/// <summary>
/// Represents a single MFE plugin entry sent to the plugin host within an <see cref="MfeSnapshot"/>.
/// Built from Docker container labels (<c>app.mfe.*</c>).
/// </summary>
public class MfePayload
{
    /// <summary>Unique identifier for the plugin, derived from the Docker Compose service name.</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable display name shown in the UI. Value of the <c>app.mfe.displayName</c> label.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Optional description. Value of the <c>app.mfe.description</c> label.</summary>
    public string? Description { get; set; }

    /// <summary>Base URL of the container hosting the plugin, derived from the Traefik router rule label.</summary>
    public required string ContainerBaseUrl { get; set; }

    /// <summary>Relative path to the plugin icon. Value of the <c>app.mfe.iconPath</c> label.</summary>
    public string? IconPath { get; set; }

    /// <summary>Relative path to the module federation entry point. Value of the <c>app.mfe.entrypointPath</c> label.</summary>
    public required string EntrypointPath { get; set; }

    /// <summary>Module name exposed by the plugin's module federation config. Value of the <c>app.mfe.exposedModule</c> label.</summary>
    public required string ExposedModule { get; set; }

    /// <summary>Frontend route under which the plugin is mounted. Value of the <c>app.mfe.route</c> label.</summary>
    public required string Route { get; set; }

    /// <summary>Whether the container is currently running or stopped.</summary>
    public MfePluginState State { get; set; } = MfePluginState.Running;
}



/// <summary>
/// Complete snapshot of all MFE containers in a Docker network, sent to the plugin host
/// on every sync call.
///
/// The host reconciles its local state from this list:
/// <list type="bullet">
///   <item>Plugins absent from the snapshot are removed (unregistered).</item>
///   <item>Plugins present but not yet known are created (registered).</item>
/// </list>
/// </summary>
/// <param name="Plugins">All MFE containers currently running in the network.</param>
public record MfeSnapshot(IReadOnlyList<MfePayload> Plugins);
