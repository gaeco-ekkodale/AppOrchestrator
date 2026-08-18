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
public interface IStackRepository
{
    /// <summary>
    /// Loads a stack by Docker project name.
    /// </summary>
    Task<Stack?> GetAsync(string dockerProjectName, CancellationToken ct);

    /// <summary>
    /// Returns all stacks, optionally filtered by registry id.
    /// </summary>
    Task<List<Stack>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts a new stack entity.
    /// </summary>
    Task AddAsync(Stack stack, CancellationToken ct);

    /// <summary>
    /// Persists updates to an existing stack entity.
    /// </summary>
    Task UpdateAsync(Stack stack, CancellationToken ct);

    /// <summary>
    /// Deletes a stack entity by Docker project name.
    /// </summary>
    Task DeleteAsync(string dockerProjectName, CancellationToken ct);
}