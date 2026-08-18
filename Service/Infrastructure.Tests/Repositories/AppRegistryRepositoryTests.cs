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

public class AppRegistryRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsRegistry()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new AppRegistryRepository(db);

        var registry = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        await sut.AddAsync(registry, CancellationToken.None);

        var stored = await db.AppRegistries.FindAsync(registry.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Main");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRegistryWithStacksIncluded()
    {
        await using var db = RepositoryTestDbContextFactory.Create();

        var registry = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        var stack = new RegistryStack
        {
            StackName = "Demo",
            DockerProjectName = "demo-project",
            NetworkName = "demo-network",
            PackageId = "demo-package",
            PackageVersion = "1.0.0",
            AppRegistry = registry,
            AppRegistryId = registry.Id
        };

        db.AppRegistries.Add(registry);
        db.Stacks.Add(stack);
        await db.SaveChangesAsync();

        var sut = new AppRegistryRepository(db);

        var result = await sut.GetByIdAsync(registry.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Stacks.Should().ContainSingle(s => s.DockerProjectName == "demo-project");
    }

    [Fact]
    public async Task GetByBaseUrlAsync_ReturnsNull_WhenMissing()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new AppRegistryRepository(db);

        var result = await sut.GetByBaseUrlAsync("https://missing.local", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsRegistriesWithStacksIncluded()
    {
        await using var db = RepositoryTestDbContextFactory.Create();

        var first = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        var second = new AppRegistry { Name = "Backup", BaseUrl = "https://backup.local" };

        db.AppRegistries.AddRange(first, second);
        db.Stacks.Add(new RegistryStack
        {
            StackName = "Demo",
            DockerProjectName = "demo-project",
            NetworkName = "demo-network",
            PackageId = "demo-package",
            PackageVersion = "1.0.0",
            AppRegistry = first,
            AppRegistryId = first.Id
        });
        await db.SaveChangesAsync();

        var sut = new AppRegistryRepository(db);

        var result = await sut.ListAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.BaseUrl == "https://registry.local" && r.Stacks.Count == 1);
        result.Should().Contain(r => r.BaseUrl == "https://backup.local" && r.Stacks.Count == 0);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenRegistryMissing()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new AppRegistryRepository(db);

        var deleted = await sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesRegistry_WhenExisting()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var registry = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        db.AppRegistries.Add(registry);
        await db.SaveChangesAsync();

        var sut = new AppRegistryRepository(db);

        var deleted = await sut.DeleteAsync(registry.Id, CancellationToken.None);

        deleted.Should().BeTrue();
        var stored = await db.AppRegistries.FindAsync(registry.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var registry = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        db.AppRegistries.Add(registry);
        await db.SaveChangesAsync();

        var sut = new AppRegistryRepository(db);
        registry.Name = "Renamed";
        registry.BaseUrl = "https://registry-v2.local";

        await sut.UpdateAsync(registry, CancellationToken.None);

        var stored = await db.AppRegistries.FindAsync(registry.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Renamed");
        stored.BaseUrl.Should().Be("https://registry-v2.local");
    }
}
