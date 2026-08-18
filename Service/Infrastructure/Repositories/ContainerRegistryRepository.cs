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
using AppOrchestrator.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppOrchestrator.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of <see cref="IContainerRegistryRepository"/>.
/// </summary>
public class ContainerRegistryRepository(AppOrchestratorDbContext db) : IContainerRegistryRepository
{
    public async Task AddAsync(ContainerRegistry registry, CancellationToken ct)
    {
        await db.ContainerRegistries.AddAsync(registry, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var registry = await db.ContainerRegistries.FindAsync([id], ct);
        if (registry is null) return false;

        db.ContainerRegistries.Remove(registry);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ContainerRegistry?> GetByServerAddressAsync(string serverAddress, CancellationToken ct)
        => await db.ContainerRegistries.FirstOrDefaultAsync(r => r.ServerAddress == serverAddress, ct);

    public async Task<ContainerRegistry?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.ContainerRegistries.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<ContainerRegistry>> ListAsync(CancellationToken ct)
        => await db.ContainerRegistries.ToListAsync(ct);

    public async Task UpdateAsync(ContainerRegistry registry, CancellationToken ct)
    {
        db.ContainerRegistries.Update(registry);
        await db.SaveChangesAsync(ct);
    }
}
