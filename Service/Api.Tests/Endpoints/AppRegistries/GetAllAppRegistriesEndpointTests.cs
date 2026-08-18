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

public class GetAllAppRegistriesEndpointTests
{
    private readonly IAppRegistryRepository _repository;
    private readonly GetAllAppRegistries _endpoint;

    public GetAllAppRegistriesEndpointTests()
    {
        _repository = Substitute.For<IAppRegistryRepository>();
        _endpoint = FastEndpoints.Factory.Create<GetAllAppRegistries>(_repository);
        AppRegistryEndpointTestHelper.InitializeAppRegistryMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllRegistries_WhenSomeExist()
    {
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppOrchestrator.Domain.Models.AppRegistry>
            {
                AppRegistryTestData.Create("A", "https://a.example"),
                AppRegistryTestData.Create("B", "https://b.example")
            });

        await _endpoint.HandleAsync(default);

        var result = _endpoint.Response.ToList();
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoRegistryExists()
    {
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppOrchestrator.Domain.Models.AppRegistry>());

        await _endpoint.HandleAsync(default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(_endpoint.Response);
    }
}
