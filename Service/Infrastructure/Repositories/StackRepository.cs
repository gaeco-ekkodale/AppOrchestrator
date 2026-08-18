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
/// Entity Framework implementation of <see cref="IStackRepository"/>.
/// </summary>
public class StackRepository(AppOrchestratorDbContext db) : IStackRepository
{
    public async Task AddAsync(Stack stack, CancellationToken ct)
    {
        await db.Stacks.AddAsync(stack, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string dockerProjectName, CancellationToken ct)
    {
        var stack = await db.Stacks.FirstOrDefaultAsync(s => s.DockerProjectName == dockerProjectName, ct);
        if (stack is not null)
        {
            db.Stacks.Remove(stack);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<Stack?> GetAsync(string dockerProjectName, CancellationToken ct)
        => await db.Stacks
        .Include(s => ((RegistryStack)s).AppRegistry)
        .Include(s => s.Network)
            .FirstOrDefaultAsync(s => s.DockerProjectName == dockerProjectName, ct);

    public async Task<List<Stack>> ListAsync(CancellationToken ct)
    {

        return await db.Stacks.Include(s => ((RegistryStack)s).AppRegistry).Include(s => s.Network).ToListAsync(ct);
    }

    public async Task UpdateAsync(Stack stack, CancellationToken ct)
    {
        db.Stacks.Update(stack);
        await db.SaveChangesAsync(ct);
    }
}
