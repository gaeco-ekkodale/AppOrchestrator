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
/// API response model for an application registry.
/// </summary>
public class AppRegistryDTO
{
    /// <summary>
    /// Registry identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the registry.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base URL used for package and file retrieval.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of stacks linked to this registry.
    /// </summary>
    public int StackCount { get; set; }

    /// <summary>
    /// True when an API key is stored for this registry. The key itself is never returned.
    /// </summary>
    public bool HasApiKey { get; set; }
}
