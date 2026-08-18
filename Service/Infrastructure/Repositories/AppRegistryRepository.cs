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
/// Entity Framework implementation of <see cref="IAppRegistryRepository"/>.
/// </summary>
public class AppRegistryRepository(AppOrchestratorDbContext db) : IAppRegistryRepository
{
    public async Task AddAsync(AppRegistry registry, CancellationToken ct)
    {
        await db.AppRegistries.AddAsync(registry, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var registry = await db.AppRegistries.FindAsync([id], ct);
        if (registry is null) return false;

        db.AppRegistries.Remove(registry);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AppRegistry?> GetByBaseUrlAsync(string baseUrl, CancellationToken ct)
        => await db.AppRegistries
            .Include(r => r.Stacks)
            .FirstOrDefaultAsync(r => r.BaseUrl == baseUrl, ct);

    public async Task<AppRegistry?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.AppRegistries
            .Include(r => r.Stacks)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<AppRegistry>> ListAsync(CancellationToken ct)
        => await db.AppRegistries
            .Include(r => r.Stacks)
            .ToListAsync(ct);

    public async Task UpdateAsync(AppRegistry registry, CancellationToken ct)
    {
        db.AppRegistries.Update(registry);
        await db.SaveChangesAsync(ct);
    }
}
