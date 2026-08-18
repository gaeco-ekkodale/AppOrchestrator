// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Text.Json.Serialization;

namespace AppOrchestrator.Api.Shared.DTOs;


/// <summary>
/// Runtime lifecycle state of a stack.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackStatus
{
    /// <summary>
    /// Current state could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Stack is currently being deployed.
    /// </summary>
    Installing,

    /// <summary>
    /// All stack containers are running.
    /// </summary>
    Running,

    /// <summary>
    /// Some but not all containers are running.
    /// </summary>
    Partial,

    /// <summary>
    /// Stack containers are stopped but resources still exist.
    /// </summary>
    Stopped,

    /// <summary>
    /// Stack is currently being updated.
    /// </summary>
    Updating,

    /// <summary>
    /// Last operation failed.
    /// </summary>
    Failed
}

/// <summary>
/// Source type of a stack entry.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackSource
{
    /// <summary>
    /// Stack deployed from an app store / registry package.
    /// </summary>
    AppStore,

    /// <summary>
    /// Stack deployed from a custom compose definition.
    /// </summary>
    CustomCompose,

    /// <summary>
    /// Stack discovered from Docker runtime without a managed DB entry.
    /// </summary>
    External
}

/// <summary>
/// API response model with stack overview information.
/// </summary>
public class StackDTO
{
    /// <summary>
    /// User-defined stack name.
    /// </summary>
    public string StackName { get; set; } = string.Empty;

    /// <summary>
    /// Docker Compose project name.
    /// </summary>
    public string DockerProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Optional id of the source app registry.
    /// </summary>
    public Guid? AppRegistryId { get; set; }

    /// <summary>
    /// Optional display name of the source app registry.
    /// </summary>
    public string? AppRegistryName { get; set; }

    /// <summary>
    /// Optional name of the network this stack is assigned to.
    /// </summary>
    public string? NetworkName { get; set; }

    /// <summary>
    /// Package identifier from the registry.
    /// </summary>
    public string? PackageId { get; set; } = string.Empty;

    /// <summary>
    /// Deployed package version.
    /// </summary>
    public string? PackageVersion { get; set; } = string.Empty;

    /// <summary>
    /// Current runtime status.
    /// </summary>
    public StackStatus Status { get; set; }

    /// <summary>
    /// Source type of the stack.
    /// </summary>
    public StackSource Source { get; set; }

    /// <summary>
    /// UTC creation timestamp.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp of the last update.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
