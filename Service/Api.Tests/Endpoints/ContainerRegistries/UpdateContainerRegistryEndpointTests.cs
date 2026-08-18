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

public class UpdateContainerRegistryEndpointTests
{
    private readonly IContainerRegistryRepository _repository;
    private readonly IDockerRegistryService _dockerRegistryService;
    private readonly UpdateContainerRegistry _endpoint;

    public UpdateContainerRegistryEndpointTests()
    {
        _repository = Substitute.For<IContainerRegistryRepository>();
        _dockerRegistryService = Substitute.For<IDockerRegistryService>();
        _endpoint = FastEndpoints.Factory.Create<UpdateContainerRegistry>(_repository, _dockerRegistryService);
        ContainerRegistryEndpointTestHelper.InitializeMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenUpdateValidAndLoginSucceeds()
    {
        var id = Guid.NewGuid();
        var existing = ContainerRegistryTestData.Create("Old", "old.azurecr.io");
        existing.Id = id;
        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();

        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.GetByServerAddressAsync("new.azurecr.io", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.ContainerRegistry?)null);
        _dockerRegistryService.LoginAsync("new.azurecr.io", "user", "secret", Arg.Any<CancellationToken>())
            .Returns((true, "Login Succeeded"));

        await _endpoint.HandleAsync(new UpdateContainerRegistryRequest
        {
            Name = "New",
            ServerAddress = "new.azurecr.io",
            Username = "user",
            Password = "secret"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("new.azurecr.io", _endpoint.Response.ServerAddress);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRegistryMissing()
    {
        var id = Guid.NewGuid();
        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.ContainerRegistry?)null);

        await _endpoint.HandleAsync(new UpdateContainerRegistryRequest
        {
            Username = "user",
            Password = "secret"
        }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenServerAddressConflicts()
    {
        var id = Guid.NewGuid();
        var existing = ContainerRegistryTestData.Create("Old", "old.azurecr.io");
        existing.Id = id;

        var other = ContainerRegistryTestData.Create("Other", "new.azurecr.io");
        other.Id = Guid.NewGuid();

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.GetByServerAddressAsync("new.azurecr.io", Arg.Any<CancellationToken>()).Returns(other);

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateContainerRegistryRequest
            {
                ServerAddress = "new.azurecr.io",
                Username = "user",
                Password = "secret"
            }, default));
    }

    [Fact]
    public async Task HandleAsync_DoesNotLogout_WhenServerAddressUnchanged()
    {
        var id = Guid.NewGuid();
        var existing = ContainerRegistryTestData.Create("Old", "same.azurecr.io");
        existing.Id = id;
        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();

        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);
        _dockerRegistryService.LoginAsync("same.azurecr.io", "user", "secret", Arg.Any<CancellationToken>())
            .Returns((true, "Login Succeeded"));
        _dockerRegistryService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Logout should not be called")));

        await _endpoint.HandleAsync(new UpdateContainerRegistryRequest
        {
            Username = "user",
            Password = "secret"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public void Validator_RejectsEmptyUsername()
    {
        var validator = new UpdateContainerRegistryValidator();
        var result = validator.Validate(new UpdateContainerRegistryRequest
        {
            Username = string.Empty,
            Password = "secret"
        });

        Assert.False(result.IsValid);
    }
}
