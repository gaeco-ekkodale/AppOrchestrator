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
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.ContainerRegistries;

public class DeleteContainerRegistryEndpointTests
{
    private readonly IContainerRegistryRepository _repository;
    private readonly IDockerRegistryService _dockerRegistryService;
    private readonly DeleteContainerRegistry _endpoint;

    public DeleteContainerRegistryEndpointTests()
    {
        _repository = Substitute.For<IContainerRegistryRepository>();
        _dockerRegistryService = Substitute.For<IDockerRegistryService>();
        _endpoint = FastEndpoints.Factory.Create<DeleteContainerRegistry>(_repository, _dockerRegistryService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoContent_WhenRegistryExists()
    {
        var id = Guid.NewGuid();
        var registry = ContainerRegistryTestData.Create("ACR", "myregistry.azurecr.io");
        registry.Id = id;

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        await _endpoint.HandleAsync(default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRegistryMissing()
    {
        var id = Guid.NewGuid();
        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.ContainerRegistry?)null);

        await _endpoint.HandleAsync(default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PropagatesException_WhenLogoutFails()
    {
        var id = Guid.NewGuid();
        var registry = ContainerRegistryTestData.Create("ACR", "myregistry.azurecr.io");
        registry.Id = id;

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);
        _dockerRegistryService.LogoutAsync("myregistry.azurecr.io", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("logout failed")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _endpoint.HandleAsync(default));
    }
}
