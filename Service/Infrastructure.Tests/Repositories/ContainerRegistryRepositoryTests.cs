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

namespace AppOrchestrator.Infrastructure.Tests.Repositories;

public class ContainerRegistryRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsRegistry()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new ContainerRegistryRepository(db);

        var registry = new ContainerRegistry { Name = "ACR", ServerAddress = "myregistry.azurecr.io" };
        await sut.AddAsync(registry, CancellationToken.None);

        var stored = await db.ContainerRegistries.FindAsync(registry.Id);
        stored.Should().NotBeNull();
        stored!.ServerAddress.Should().Be("myregistry.azurecr.io");
    }

    [Fact]
    public async Task GetByServerAddressAsync_ReturnsRegistry_WhenExisting()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var registry = new ContainerRegistry { Name = "ACR", ServerAddress = "myregistry.azurecr.io" };
        db.ContainerRegistries.Add(registry);
        await db.SaveChangesAsync();

        var sut = new ContainerRegistryRepository(db);

        var result = await sut.GetByServerAddressAsync("myregistry.azurecr.io", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("ACR");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new ContainerRegistryRepository(db);

        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsAllRegistries()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        db.ContainerRegistries.AddRange(
            new ContainerRegistry { Name = "ACR", ServerAddress = "myregistry.azurecr.io" },
            new ContainerRegistry { Name = "GHCR", ServerAddress = "ghcr.io" });
        await db.SaveChangesAsync();

        var sut = new ContainerRegistryRepository(db);

        var result = await sut.ListAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.ServerAddress == "ghcr.io");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenRegistryMissing()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new ContainerRegistryRepository(db);

        var deleted = await sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesRegistry_WhenExisting()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var registry = new ContainerRegistry { Name = "ACR", ServerAddress = "myregistry.azurecr.io" };
        db.ContainerRegistries.Add(registry);
        await db.SaveChangesAsync();

        var sut = new ContainerRegistryRepository(db);

        var deleted = await sut.DeleteAsync(registry.Id, CancellationToken.None);

        deleted.Should().BeTrue();
        var stored = await db.ContainerRegistries.FindAsync(registry.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var registry = new ContainerRegistry { Name = "ACR", ServerAddress = "myregistry.azurecr.io" };
        db.ContainerRegistries.Add(registry);
        await db.SaveChangesAsync();

        var sut = new ContainerRegistryRepository(db);
        registry.Name = "ACR-Prod";
        registry.ServerAddress = "prod.azurecr.io";

        await sut.UpdateAsync(registry, CancellationToken.None);

        var stored = await db.ContainerRegistries.FindAsync(registry.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("ACR-Prod");
        stored.ServerAddress.Should().Be("prod.azurecr.io");
    }
}
