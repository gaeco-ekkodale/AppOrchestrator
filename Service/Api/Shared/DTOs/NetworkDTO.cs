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
/// API response model for a user-created Docker network.
/// </summary>
public class NetworkDTO
{
    /// <summary>
    /// Docker network name – the immutable primary key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the network was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp of the last metadata update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Shared environment variables injected into every stack on this network.
    /// </summary>
    public List<EnvironmentVariableDTO> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Version suffixes that restrict update notifications for stacks on this network.
    /// Empty means no restriction. Example: ["test", "staging"].
    /// </summary>
    public List<string> AllowedVersionSuffixes { get; set; } = [];

    /// <summary>
    /// Stacks currently assigned to this network (lightweight summary).
    /// </summary>
    public List<NetworkStackSummary> Stacks { get; set; } = [];
}

/// <summary>
/// Key-value pair representing a shared network environment variable.
/// </summary>
public class EnvironmentVariableDTO
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Lightweight stack summary embedded inside <see cref="NetworkDTO"/>.
/// </summary>
public class NetworkStackSummary
{
    /// <summary>
    /// User-defined stack name.
    /// </summary>
    public string StackName { get; set; } = string.Empty;

    /// <summary>
    /// Docker Compose project name.
    /// </summary>
    public string DockerProjectName { get; set; } = string.Empty;
}
