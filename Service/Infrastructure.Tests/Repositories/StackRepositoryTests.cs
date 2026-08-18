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

public class StackRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsStack()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var sut = new StackRepository(db);
        var stack = CreateRegistryStack("my-project");

        await sut.AddAsync(stack, CancellationToken.None);

        var stored = await db.Stacks.FindAsync(stack.Id);
        stored.Should().NotBeNull();
        stored!.DockerProjectName.Should().Be("my-project");
    }

    [Fact]
    public async Task GetAsync_ReturnsStackWithIncludedRelations()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var appRegistry = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        var network = new Network { Name = "shared-net" };
        var stack = CreateRegistryStack("my-project");
        stack.AppRegistry = appRegistry;
        stack.AppRegistryId = appRegistry.Id;
        stack.Network = network;
        stack.NetworkName = network.Name;

        db.AppRegistries.Add(appRegistry);
        db.Networks.Add(network);
        db.Stacks.Add(stack);
        await db.SaveChangesAsync();

        var sut = new StackRepository(db);

        var result = await sut.GetAsync("my-project", CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<RegistryStack>();
        var registryResult = (RegistryStack)result!;
        registryResult.AppRegistry.Should().NotBeNull();
        registryResult.AppRegistry!.BaseUrl.Should().Be("https://registry.local");
        registryResult.Network.Should().NotBeNull();
        registryResult.Network!.Name.Should().Be("shared-net");
    }

    [Fact]
    public async Task ListAsync_ReturnsStacksWithIncludedRelations()
    {
        await using var db = RepositoryTestDbContextFactory.Create();

        var appRegistry = new AppRegistry { Name = "Main", BaseUrl = "https://registry.local" };
        var network = new Network { Name = "shared-net" };

        var first = CreateRegistryStack("project-a");
        first.AppRegistry = appRegistry;
        first.AppRegistryId = appRegistry.Id;
        first.Network = network;
        first.NetworkName = network.Name;

        var second = CreateCustomStack("project-b");

        db.AppRegistries.Add(appRegistry);
        db.Networks.Add(network);
        db.Stacks.AddRange(first, second);
        await db.SaveChangesAsync();

        var sut = new StackRepository(db);

        var result = await sut.ListAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.DockerProjectName == "project-a" && x is RegistryStack && x.Network != null);
        result.Should().Contain(x => x.DockerProjectName == "project-b");
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenStackDoesNotExist()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        db.Stacks.Add(CreateRegistryStack("existing-project"));
        await db.SaveChangesAsync();

        var sut = new StackRepository(db);

        await sut.DeleteAsync("missing-project", CancellationToken.None);

        var stacks = await db.Stacks.ToListAsync();
        stacks.Should().ContainSingle();
        stacks[0].DockerProjectName.Should().Be("existing-project");
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangedFields()
    {
        await using var db = RepositoryTestDbContextFactory.Create();
        var stack = CreateRegistryStack("my-project");

        db.Stacks.Add(stack);
        await db.SaveChangesAsync();

        var sut = new StackRepository(db);
        stack.PackageVersion = "2.0.0";
        stack.StackName = "Renamed";

        await sut.UpdateAsync(stack, CancellationToken.None);

        var stored = await db.Stacks.FindAsync(stack.Id);
        stored.Should().NotBeNull();
        stored.Should().BeOfType<RegistryStack>();
        ((RegistryStack)stored!).PackageVersion.Should().Be("2.0.0");
        stored.StackName.Should().Be("Renamed");
    }

    private static RegistryStack CreateRegistryStack(string projectName)
        => new()
        {
            StackName = "Demo Stack",
            DockerProjectName = projectName,
            NetworkName = "default",
            AppRegistryId = Guid.NewGuid(),
            PackageId = "demo-package",
            PackageVersion = "1.0.0"
        };

    private static CustomStack CreateCustomStack(string projectName)
        => new()
        {
            StackName = "Custom Stack",
            DockerProjectName = projectName,
            NetworkName = "default"
        };
}
