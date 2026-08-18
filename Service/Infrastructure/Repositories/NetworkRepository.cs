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
/// Entity Framework implementation of <see cref="INetworkRepository"/>.
/// </summary>
public class NetworkRepository(AppOrchestratorDbContext db) : INetworkRepository
{
    public async Task<Network?> GetByNameAsync(string name, CancellationToken ct)
        => await db.Networks.Include(n => n.Stacks).FirstOrDefaultAsync(n => n.Name == name, ct);

    public async Task<List<Network>> ListAsync(CancellationToken ct)
        => await db.Networks.Include(n => n.Stacks).ToListAsync(ct);

    public async Task AddAsync(Network network, CancellationToken ct)
    {
        await db.Networks.AddAsync(network, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Network network, CancellationToken ct)
    {
        db.Networks.Update(network);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string name, CancellationToken ct)
    {
        var network = await db.Networks.FirstOrDefaultAsync(n => n.Name == name, ct);
        if (network is not null)
        {
            db.Networks.Remove(network);
            await db.SaveChangesAsync(ct);
        }
    }
}
