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

public class CreateAppRegistryEndpointTests
{
    private readonly IAppRegistryRepository _repository;
    private readonly IRegistrySecretProtector _secretProtector;
    private readonly CreateAppRegistry _endpoint;

    public CreateAppRegistryEndpointTests()
    {
        _repository = Substitute.For<IAppRegistryRepository>();
        _secretProtector = Substitute.For<IRegistrySecretProtector>();
        _endpoint = Factory.Create<CreateAppRegistry>(_repository, _secretProtector);
        AppRegistryEndpointTestHelper.InitializeAppRegistryMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenBaseUrlIsUnique()
    {
        _repository.GetByBaseUrlAsync("https://registry.example", Arg.Any<CancellationToken>())
            .Returns((AppRegistry?)null);

        await _endpoint.HandleAsync(new CreateAppRegistryRequest
        {
            Name = "Main Registry",
            BaseUrl = "https://registry.example"
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("Main Registry", _endpoint.Response.Name);
    }

    [Fact]
    public async Task HandleAsync_SetsHasApiKeyTrue_WhenApiKeyProvided()
    {
        _repository.GetByBaseUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AppRegistry?)null);
        _secretProtector.Protect("secret-key-123").Returns("encrypted-blob");

        await _endpoint.HandleAsync(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "https://registry.example",
            ApiKey = "secret-key-123"
        }, default);

        Assert.True(_endpoint.Response.HasApiKey);
    }

    [Fact]
    public async Task HandleAsync_SetsHasApiKeyFalse_WhenNoApiKeyProvided()
    {
        _repository.GetByBaseUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AppRegistry?)null);

        await _endpoint.HandleAsync(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "https://registry.example"
        }, default);

        Assert.False(_endpoint.Response.HasApiKey);
    }

    [Fact]
    public async Task HandleAsync_StoresEncryptedKey_NotPlaintext_WhenApiKeyProvided()
    {
        AppRegistry? saved = null;
        _repository.GetByBaseUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AppRegistry?)null);
        _repository.AddAsync(
            Arg.Do<AppRegistry>(r => saved = r),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _secretProtector.Protect("raw-key").Returns("encrypted-blob");

        await _endpoint.HandleAsync(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "https://registry.example",
            ApiKey = "raw-key"
        }, default);

        // The stored value must be the protector's output, never the raw plaintext.
        Assert.Equal("encrypted-blob", saved?.ApiKeyEncrypted);
        Assert.NotEqual("raw-key", saved?.ApiKeyEncrypted);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenBaseUrlAlreadyExists()
    {
        _repository.GetByBaseUrlAsync("https://registry.example", Arg.Any<CancellationToken>())
            .Returns(AppRegistryTestData.Create(baseUrl: "https://registry.example"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateAppRegistryRequest
            {
                Name = "Main Registry",
                BaseUrl = "https://registry.example"
            }, default));
    }

    [Fact]
    public async Task HandleAsync_PropagatesRepositoryFailure_WhenAddFails()
    {
        _repository.GetByBaseUrlAsync("https://registry.example", Arg.Any<CancellationToken>())
            .Returns((AppRegistry?)null);

        _repository.AddAsync(Arg.Any<AppRegistry>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("db failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _endpoint.HandleAsync(new CreateAppRegistryRequest
            {
                Name = "Main Registry",
                BaseUrl = "https://registry.example"
            }, default));
    }

    [Fact]
    public void Validator_RejectsNonHttpBaseUrl()
    {
        var validator = new CreateAppRegistryValidator();
        var result = validator.Validate(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "ftp://registry.example"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsEmptyStringApiKey()
    {
        var validator = new CreateAppRegistryValidator();
        var result = validator.Validate(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "https://registry.example",
            ApiKey = string.Empty
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ApiKey");
    }

    [Fact]
    public void Validator_AcceptsNullApiKey()
    {
        var validator = new CreateAppRegistryValidator();
        var result = validator.Validate(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "https://registry.example",
            ApiKey = null
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsApiKeyExceedingMaxLength()
    {
        var validator = new CreateAppRegistryValidator();
        var result = validator.Validate(new CreateAppRegistryRequest
        {
            Name = "Registry",
            BaseUrl = "https://registry.example",
            ApiKey = new string('x', 501)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ApiKey");
    }
}
