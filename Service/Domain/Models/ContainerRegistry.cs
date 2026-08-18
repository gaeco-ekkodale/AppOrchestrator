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
/// Represents a container registry that the orchestrator can pull images from.
/// Credentials are NOT stored - they are passed through to docker login at registration time.
/// </summary>
public class ContainerRegistry
{
    /// <summary>
    /// Primary key of the container registry entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of the registry entry.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// The registry server address, e.g. "myregistry.azurecr.io".
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string ServerAddress { get; set; }

    /// <summary>
    /// UTC timestamp when the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
