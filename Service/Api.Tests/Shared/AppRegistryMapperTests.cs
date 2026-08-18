// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Domain.Models;

namespace AppOrchestrator.Api.Tests.Shared;

public class AppRegistryMapperTests
{
    private readonly AppRegistryMapper _mapper = new();

    [Fact]
    public void FromEntity_SetsHasApiKeyTrue_WhenEncryptedKeyIsSet()
    {
        var registry = Registry(apiKeyEncrypted: "some-encrypted-blob");
        var dto = _mapper.FromEntity(registry);
        Assert.True(dto.HasApiKey);
    }

    [Fact]
    public void FromEntity_SetsHasApiKeyFalse_WhenEncryptedKeyIsNull()
    {
        var registry = Registry(apiKeyEncrypted: null);
        var dto = _mapper.FromEntity(registry);
        Assert.False(dto.HasApiKey);
    }

    [Fact]
    public void FromEntity_SetsHasApiKeyFalse_WhenEncryptedKeyIsEmpty()
    {
        var registry = Registry(apiKeyEncrypted: string.Empty);
        var dto = _mapper.FromEntity(registry);
        Assert.False(dto.HasApiKey);
    }

    [Fact]
    public void FromEntity_MapsIdentityFields()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var registry = new AppRegistry
        {
            Id = id,
            Name = "My Registry",
            BaseUrl = "https://registry.example",
            CreatedAt = createdAt,
            ApiKeyEncrypted = null,
            Stacks = []
        };

        var dto = _mapper.FromEntity(registry);

        Assert.Equal(id, dto.Id);
        Assert.Equal("My Registry", dto.Name);
        Assert.Equal("https://registry.example", dto.BaseUrl);
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    [Fact]
    public void FromEntity_CountsLinkedStacks()
    {
        var registry = Registry();
        registry.Stacks =
        [
            new RegistryStack { PackageId = "pkg-a", PackageVersion = "1.0", StackName = "a", DockerProjectName = "a", NetworkName = "net" },
            new RegistryStack { PackageId = "pkg-b", PackageVersion = "2.0", StackName = "b", DockerProjectName = "b", NetworkName = "net" }
        ];

        var dto = _mapper.FromEntity(registry);

        Assert.Equal(2, dto.StackCount);
    }

    [Fact]
    public void FromEntity_ReturnsZeroStackCount_WhenStacksIsEmpty()
    {
        var registry = Registry();
        registry.Stacks = [];

        var dto = _mapper.FromEntity(registry);

        Assert.Equal(0, dto.StackCount);
    }

    private static AppRegistry Registry(string? apiKeyEncrypted = null) => new()
    {
        Name = "Test",
        BaseUrl = "https://registry.example",
        ApiKeyEncrypted = apiKeyEncrypted,
        Stacks = []
    };
}
