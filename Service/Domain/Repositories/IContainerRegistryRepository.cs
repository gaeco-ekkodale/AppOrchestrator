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
/// Repository contract for container registry persistence operations.
/// </summary>
public interface IContainerRegistryRepository
{
    /// <summary>
    /// Loads a registry by its identifier.
    /// </summary>
    Task<ContainerRegistry?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Loads a registry by its Docker server address.
    /// </summary>
    Task<ContainerRegistry?> GetByServerAddressAsync(string serverAddress, CancellationToken ct);

    /// <summary>
    /// Returns all container registries.
    /// </summary>
    Task<List<ContainerRegistry>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts a new container registry.
    /// </summary>
    Task AddAsync(ContainerRegistry registry, CancellationToken ct);

    /// <summary>
    /// Persists updates to an existing container registry.
    /// </summary>
    Task UpdateAsync(ContainerRegistry registry, CancellationToken ct);

    /// <summary>
    /// Deletes a registry by id.
    /// Returns true if an entity was deleted.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
