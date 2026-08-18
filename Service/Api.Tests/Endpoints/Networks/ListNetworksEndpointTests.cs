// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.Networks;
using AppOrchestrator.Domain.Repositories;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Networks;

public class ListNetworksEndpointTests
{
    private readonly INetworkRepository _networkRepository;
    private readonly ListNetworks _endpoint;

    public ListNetworksEndpointTests()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _endpoint = FastEndpoints.Factory.Create<ListNetworks>(_networkRepository);
        NetworkEndpointTestHelper.InitializeNetworkMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllNetworks_WhenSomeExist()
    {
        _networkRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppOrchestrator.Domain.Models.Network>
            {
                NetworkTestData.Create("dev"),
                NetworkTestData.Create("prod")
            });

        await _endpoint.HandleAsync(default);

        var result = _endpoint.Response.ToList();
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoNetworksExist()
    {
        _networkRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppOrchestrator.Domain.Models.Network>());

        await _endpoint.HandleAsync(default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(_endpoint.Response);
    }
}
