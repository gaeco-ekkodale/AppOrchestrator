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
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.AppRegistries;

public class DeleteAppRegistryEndpointTests
{
    private readonly IAppRegistryRepository _repository;
    private readonly DeleteAppRegistry _endpoint;

    public DeleteAppRegistryEndpointTests()
    {
        _repository = Substitute.For<IAppRegistryRepository>();
        _endpoint = FastEndpoints.Factory.Create<DeleteAppRegistry>(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoContent_WhenRegistryHasNoStacks()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create();
        registry.Id = id;
        registry.Stacks = new List<RegistryStack>();

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
            .Returns((AppOrchestrator.Domain.Models.AppRegistry?)null);

        await _endpoint.HandleAsync(default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenRegistryStillReferenced()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create();
        registry.Id = id;
        registry.Stacks = new List<RegistryStack> { new() { StackName = "s", DockerProjectName = "stack-s", PackageId = "pkg", PackageVersion = "1.0.0", AppRegistryId = id, NetworkName = "default" } };

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        await Assert.ThrowsAsync<ValidationFailureException>(() => _endpoint.HandleAsync(default));
    }
}
