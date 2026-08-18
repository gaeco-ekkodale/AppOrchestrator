// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;

namespace AppOrchestrator.Domain.Models;

/// <summary>
/// Represents a user-created Docker network managed by the orchestrator.
/// Only networks explicitly created through the API are persisted here.
/// </summary>
public class Network
{
    /// <summary>
    /// Primary key – the Docker network name. Immutable after creation.
    /// </summary>
    [Key, Required, MaxLength(256)]
    public required string Name { get; set; }

    /// <summary>
    /// UTC timestamp when the network entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last metadata update.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Shared environment variables injected into every stack deployed on this network.
    /// </summary>
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Version suffixes that are permitted as update targets for stacks on this network.
    /// When empty, all versions are considered. When set, only versions whose string
    /// contains <c>-{suffix}</c> are shown as available updates.
    /// Example: ["test", "staging"] allows "1.0.0-test" and "1.0.0-staging".
    /// </summary>
    public List<AllowedVersionSuffix> AllowedVersionSuffixes { get; set; } = [];

    /// <summary>
    /// Stacks that are assigned to this network.
    /// </summary>
    public virtual ICollection<Stack> Stacks { get; set; } = [];
}

/// <summary>
/// Represents an environment variable key-value pair associated with a Network
/// for shared configuration across stacks connected to the same network.
/// Configured as an EF Core owned type on <see cref="Network"/>.
/// </summary>
public class EnvironmentVariable
{
    /// <summary>
    /// Variable name (e.g. <c>DATABASE_HOST</c>).
    /// </summary>
    [Required, MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Variable value.
    /// </summary>
    [Required, MaxLength(500)]
    public required string Value { get; set; }
}

/// <summary>
/// A single version-suffix entry on a <see cref="Network"/> that controls
/// which package versions are eligible as update notifications for stacks on this network.
/// Configured as an EF Core owned type on <see cref="Network"/>.
/// </summary>
public class AllowedVersionSuffix
{
    /// <summary>
    /// The suffix string (e.g. <c>test</c>, <c>staging</c>, <c>customer-acme</c>).
    /// A version is considered eligible when its version string contains <c>-{Suffix}</c>.
    /// </summary>
    [Required, MaxLength(100)]
    public required string Suffix { get; set; }
}
