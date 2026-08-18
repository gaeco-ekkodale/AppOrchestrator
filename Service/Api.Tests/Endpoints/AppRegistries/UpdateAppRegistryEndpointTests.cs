// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.AppRegistries;
using AppOrchestrator.Api.Services._Interfaces;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.AppRegistries;

public class UpdateAppRegistryEndpointTests
{
    private readonly IAppRegistryRepository _repository;
    private readonly IRegistrySecretProtector _secretProtector;
    private readonly UpdateAppRegistry _endpoint;

    public UpdateAppRegistryEndpointTests()
    {
        _repository = Substitute.For<IAppRegistryRepository>();
        _secretProtector = Substitute.For<IRegistrySecretProtector>();
        _endpoint = Factory.Create<UpdateAppRegistry>(_repository, _secretProtector);
        AppRegistryEndpointTestHelper.InitializeAppRegistryMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenPatchIsValid()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Old", "https://old.example");
        registry.Id = id;

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        await _endpoint.HandleAsync(new UpdateAppRegistryRequest
        {
            Name = "New",
            BaseUrl = "https://new.example"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("New", _endpoint.Response.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRegistryMissing()
    {
        var id = Guid.NewGuid();
        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((AppRegistry?)null);

        await _endpoint.HandleAsync(new UpdateAppRegistryRequest { Name = "New" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenBaseUrlUsedByAnotherRegistry()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Old", "https://old.example");
        registry.Id = id;

        var other = AppRegistryTestData.Create("Other", "https://new.example");
        other.Id = Guid.NewGuid();

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);
        _repository.GetByBaseUrlAsync("https://new.example", Arg.Any<CancellationToken>()).Returns(other);

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateAppRegistryRequest { BaseUrl = "https://new.example" }, default));
    }

    [Fact]
    public async Task HandleAsync_DoesNotCheckBaseUrlUniqueness_WhenBaseUrlUnchanged()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Old", "https://same.example");
        registry.Id = id;

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        await _endpoint.HandleAsync(new UpdateAppRegistryRequest { BaseUrl = "https://same.example" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("https://same.example", _endpoint.Response.BaseUrl);
    }

    [Fact]
    public async Task HandleAsync_ClearsApiKey_WhenEmptyStringProvided()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Registry", "https://registry.example");
        registry.Id = id;
        registry.ApiKeyEncrypted = "previously-stored-key";

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        await _endpoint.HandleAsync(new UpdateAppRegistryRequest { ApiKey = string.Empty }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.False(_endpoint.Response.HasApiKey);
    }

    [Fact]
    public async Task HandleAsync_PreservesApiKey_WhenApiKeyFieldIsNull()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Registry", "https://registry.example");
        registry.Id = id;
        registry.ApiKeyEncrypted = "previously-stored-key";

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        // null means "don't touch the stored key" — the existing key must survive
        await _endpoint.HandleAsync(new UpdateAppRegistryRequest { ApiKey = null }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.True(_endpoint.Response.HasApiKey);
    }

    [Fact]
    public async Task HandleAsync_StoresEncryptedNewKey_WhenApiKeyUpdated()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Registry", "https://registry.example");
        registry.Id = id;

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);
        _secretProtector.Protect("new-key").Returns("new-encrypted-blob");

        AppRegistry? saved = null;
        _repository.UpdateAsync(
            Arg.Do<AppRegistry>(r => saved = r),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _endpoint.HandleAsync(new UpdateAppRegistryRequest { ApiKey = "new-key" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.True(_endpoint.Response.HasApiKey);
        // The update must store the protector's output, not the plaintext key.
        Assert.Equal("new-encrypted-blob", saved?.ApiKeyEncrypted);
    }

    [Fact]
    public void Validator_RejectsEmptyNameWhenProvided()
    {
        var validator = new UpdateAppRegistryValidator();
        var result = validator.Validate(new UpdateAppRegistryRequest
        {
            Name = string.Empty
        });

        Assert.False(result.IsValid);
    }
}
