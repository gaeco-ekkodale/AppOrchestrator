// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Models;

namespace AppOrchestrator.Domain.Repositories;

/// <summary>
/// Repository interface for application data access operations.
/// Provides methods to manage application entities in the database.
/// </summary>
public interface IAppRegistryRepository
{
    /// <summary>
    /// Loads a registry by id.
    /// </summary>
    Task<AppRegistry?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Loads a registry by base URL.
    /// </summary>
    Task<AppRegistry?> GetByBaseUrlAsync(string baseUrl, CancellationToken ct);

    /// <summary>
    /// Returns all known registries.
    /// </summary>
    Task<List<AppRegistry>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts a new registry.
    /// </summary>
    Task AddAsync(AppRegistry registry, CancellationToken ct);

    /// <summary>
    /// Persists updates to an existing registry.
    /// </summary>
    Task UpdateAsync(AppRegistry registry, CancellationToken ct);

    /// <summary>
    /// Deletes a registry by id.
    /// Returns true if an entity was deleted.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}