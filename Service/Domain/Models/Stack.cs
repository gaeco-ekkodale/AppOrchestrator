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
/// Abstract base for every persisted stack (registry-managed or custom compose).
/// Uses EF Core TPH (Table-Per-Hierarchy) with a <c>StackType</c> discriminator column.
/// </summary>
public abstract class Stack
{
    /// <summary>
    /// Primary key of the stack entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User-facing stack name.
    /// </summary>
    [Required, MaxLength(256)]
    public required string StackName { get; set; }

    /// <summary>
    /// Docker Compose project name derived from <see cref="StackName"/>.
    /// </summary>
    [Required, MaxLength(128)]
    public required string DockerProjectName { get; set; }

    /// <summary>
    /// Foreign key to the network this stack belongs to (the Docker network name).
    /// </summary>
    [MaxLength(256)]
    public required string NetworkName { get; set; }

    /// <summary>
    /// Navigation property to the network this stack belongs to.
    /// </summary>
    public Network? Network { get; set; }

    /// <summary>
    /// UTC timestamp when the stack entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last metadata update.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}