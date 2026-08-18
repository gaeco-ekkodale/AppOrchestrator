// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.ContainerRegistries;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.ContainerRegistries;

public class CreateContainerRegistryEndpointTests
{
    private readonly IContainerRegistryRepository _repository;
    private readonly IDockerRegistryService _dockerRegistryService;
    private readonly CreateContainerRegistry _endpoint;

    public CreateContainerRegistryEndpointTests()
    {
        _repository = Substitute.For<IContainerRegistryRepository>();
        _dockerRegistryService = Substitute.For<IDockerRegistryService>();
        _endpoint = FastEndpoints.Factory.Create<CreateContainerRegistry>(_repository, _dockerRegistryService);
        ContainerRegistryEndpointTestHelper.InitializeMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenAddressUniqueAndLoginSuccessful()
    {
        _repository.GetByServerAddressAsync("myregistry.azurecr.io", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.ContainerRegistry?)null);
        _dockerRegistryService.LoginAsync("myregistry.azurecr.io", "user", "secret", Arg.Any<CancellationToken>())
            .Returns((true, "Login Succeeded"));

        await _endpoint.HandleAsync(new CreateContainerRegistryRequest
        {
            Name = "ACR",
            ServerAddress = "myregistry.azurecr.io",
            Username = "user",
            Password = "secret"
        }, default);
        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenAddressAlreadyExists()
    {
        _repository.GetByServerAddressAsync("myregistry.azurecr.io", Arg.Any<CancellationToken>())
            .Returns(ContainerRegistryTestData.Create(serverAddress: "myregistry.azurecr.io"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateContainerRegistryRequest
            {
                Name = "ACR",
                ServerAddress = "myregistry.azurecr.io",
                Username = "user",
                Password = "secret"
            }, default));
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenDockerLoginFails()
    {
        _repository.GetByServerAddressAsync("myregistry.azurecr.io", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.ContainerRegistry?)null);
        _dockerRegistryService.LoginAsync("myregistry.azurecr.io", "user", "bad", Arg.Any<CancellationToken>())
            .Returns((false, "unauthorized"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateContainerRegistryRequest
            {
                Name = "ACR",
                ServerAddress = "myregistry.azurecr.io",
                Username = "user",
                Password = "bad"
            }, default));
    }

    [Fact]
    public void Validator_RejectsInvalidServerAddress()
    {
        var validator = new CreateContainerRegistryValidator();
        var result = validator.Validate(new CreateContainerRegistryRequest
        {
            Name = "ACR",
            ServerAddress = "invalid address",
            Username = "user",
            Password = "secret"
        });

        Assert.False(result.IsValid);
    }
}
