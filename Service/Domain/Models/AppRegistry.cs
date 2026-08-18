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
/// Represents an external application registry that serves deployable packages.
/// </summary>
public class AppRegistry
{

    /// <summary>
    /// Primary key of the registry entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Base URL of the registry API used to resolve package files.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Display name of the registry shown in clients.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// UTC timestamp when the registry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// API key stored as a Data Protection encrypted blob. Null means no key is configured.
    /// Never expose this value in API responses.
    /// </summary>
    [MaxLength(1024)]
    public string? ApiKeyEncrypted { get; set; }

    /// <summary>
    /// Registry stack entities currently linked to this registry.
    /// </summary>
    public virtual ICollection<RegistryStack> Stacks { get; set; } = [];
}
