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
using AppOrchestrator.Domain.Repositories;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.ContainerRegistries;

public class GetAllContainerRegistriesEndpointTests
{
    private readonly IContainerRegistryRepository _repository;
    private readonly GetAllContainerRegistries _endpoint;

    public GetAllContainerRegistriesEndpointTests()
    {
        _repository = Substitute.For<IContainerRegistryRepository>();
        _endpoint = FastEndpoints.Factory.Create<GetAllContainerRegistries>(_repository);
        ContainerRegistryEndpointTestHelper.InitializeMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllEntries_WhenSomeExist()
    {
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppOrchestrator.Domain.Models.ContainerRegistry>
            {
                ContainerRegistryTestData.Create("ACR", "a.azurecr.io"),
                ContainerRegistryTestData.Create("GHCR", "ghcr.io")
            });

        await _endpoint.HandleAsync(default);

        var result = _endpoint.Response.ToList();
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoneExist()
    {
        _repository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppOrchestrator.Domain.Models.ContainerRegistry>());

        await _endpoint.HandleAsync(default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(_endpoint.Response);
    }
}
