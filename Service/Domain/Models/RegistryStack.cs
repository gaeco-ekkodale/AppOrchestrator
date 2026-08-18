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
/// A stack deployed from a versioned package in an <see cref="AppRegistry"/>.
/// Compose editing is blocked; updates are done via version bumps.
/// </summary>
public class RegistryStack : Stack
{
    /// <summary>
    /// Foreign key to the source application registry.
    /// </summary>
    public Guid AppRegistryId { get; set; }

    /// <summary>
    /// Navigation property to the source application registry.
    /// </summary>
    public AppRegistry AppRegistry { get; set; } = null!;

    /// <summary>
    /// Package identifier in the source registry.
    /// </summary>
    [Required, MaxLength(256)]
    public required string PackageId { get; set; }

    /// <summary>
    /// Package version deployed for this stack.
    /// </summary>
    [Required, MaxLength(50)]
    public required string PackageVersion { get; set; }
}
