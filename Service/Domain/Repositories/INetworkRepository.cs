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
/// Repository interface for Docker network data access operations.
/// </summary>
public interface INetworkRepository
{
    /// <summary>
    /// Returns a network by its name (primary key).
    /// </summary>
    Task<Network?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>
    /// Returns all stored networks.
    /// </summary>
    Task<List<Network>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts a new network entity.
    /// </summary>
    Task AddAsync(Network network, CancellationToken ct);

    /// <summary>
    /// Persists changes to an existing network entity.
    /// </summary>
    Task UpdateAsync(Network network, CancellationToken ct);

    /// <summary>
    /// Deletes a network entity by its name.
    /// </summary>
    Task DeleteAsync(string name, CancellationToken ct);
}
