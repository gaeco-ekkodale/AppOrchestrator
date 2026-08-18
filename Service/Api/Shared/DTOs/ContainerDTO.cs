// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Shared.DTOs;

/// <summary>
/// Represents a single Docker container that belongs to a compose stack.
/// </summary>
public class ContainerDTO
{
    /// <summary>Short Docker container ID (12 characters).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Container name without the leading slash.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Compose service name (label com.docker.compose.service).</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Image name and tag.</summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>Docker container state: running, exited, paused, restarting, dead, created.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Human-readable status string returned by Docker, e.g. "Up 2 hours".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Published port mappings, e.g. "0.0.0.0:8080->80/tcp".</summary>
    public List<string> Ports { get; set; } = [];

    /// <summary>
    /// Frontend URL derived from the Traefik router rule label
    /// (<c>traefik.http.routers.&lt;name&gt;.rule=Host(`hostname`)</c>).
    /// <c>null</c> when no Traefik router rule label is present on the container.
    /// </summary>
    public string? TraefikUrl { get; set; }
}
