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
using AppOrchestrator.Domain.Repositories;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.AppRegistries;

public class GetAppRegistryEndpointTests
{
    private readonly IAppRegistryRepository _repository;
    private readonly GetAppRegistry _endpoint;

    public GetAppRegistryEndpointTests()
    {
        _repository = Substitute.For<IAppRegistryRepository>();
        _endpoint = FastEndpoints.Factory.Create<GetAppRegistry>(_repository);
        AppRegistryEndpointTestHelper.InitializeAppRegistryMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRegistry_WhenIdExists()
    {
        var id = Guid.NewGuid();
        var registry = AppRegistryTestData.Create("Main", "https://registry.example");
        registry.Id = id;

        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(registry);

        await _endpoint.HandleAsync(default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(id, _endpoint.Response.Id);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenIdMissingInRepository()
    {
        var id = Guid.NewGuid();
        _endpoint.HttpContext.Request.RouteValues["id"] = id.ToString();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.AppRegistry?)null);

        await _endpoint.HandleAsync(default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }
}
