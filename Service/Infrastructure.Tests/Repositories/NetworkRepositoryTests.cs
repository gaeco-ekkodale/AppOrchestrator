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
using AppOrchestrator.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AppOrchestrator.Infrastructure.Tests.Repositories;

public class NetworkRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsNetwork()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new NetworkRepository(db);

        await sut.AddAsync(new Network { Name = "backend-net" }, CancellationToken.None);

        var stored = await db.Networks.FirstOrDefaultAsync(n => n.Name == "backend-net");
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNetworkWithStacksIncluded()
    {
        await using var db = RepositoryTestDbContextFactory.Create();

        var network = new Network { Name = "backend-net" };
        var stack = new CustomStack
        {
            StackName = "Demo",
            DockerProjectName = "demo-project",
            NetworkName = "backend-net"
        };

        db.Networks.Add(network);
        db.Stacks.Add(stack);
        await db.SaveChangesAsync();

        var sut = new NetworkRepository(db);

        var result = await sut.GetByNameAsync("backend-net", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Stacks.Should().ContainSingle(s => s.DockerProjectName == "demo-project");
    }

    [Fact]
    public async Task ListAsync_ReturnsAllNetworksWithStacksIncluded()
    {
        await using var db = RepositoryTestDbContextFactory.Create();

        db.Networks.AddRange(new Network { Name = "net-a" }, new Network { Name = "net-b" });
        db.Stacks.Add(new CustomStack
        {
            StackName = "Demo",
            DockerProjectName = "demo-project",
            NetworkName = "net-a"
        });
        await db.SaveChangesAsync();

        var sut = new NetworkRepository(db);

        var result = await sut.ListAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Name == "net-a" && n.Stacks.Count == 1);
        result.Should().Contain(n => n.Name == "net-b" && n.Stacks.Count == 0);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingNetwork()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        db.Networks.Add(new Network { Name = "backend-net" });
        await db.SaveChangesAsync();

        var sut = new NetworkRepository(db);

        await sut.DeleteAsync("backend-net", CancellationToken.None);

        var stored = await db.Networks.FirstOrDefaultAsync(n => n.Name == "backend-net");
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenNetworkMissing()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        db.Networks.Add(new Network { Name = "existing-net" });
        await db.SaveChangesAsync();

        var sut = new NetworkRepository(db);

        await sut.DeleteAsync("missing-net", CancellationToken.None);

        var all = await db.Networks.ToListAsync();
        all.Should().ContainSingle(n => n.Name == "existing-net");
    }
}
