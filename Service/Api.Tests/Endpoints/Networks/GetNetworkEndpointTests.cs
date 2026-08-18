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
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Networks;

public class GetNetworkEndpointTests
{
    private readonly INetworkRepository _networkRepository;
    private readonly GetNetwork _endpoint;

    public GetNetworkEndpointTests()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _endpoint = FastEndpoints.Factory.Create<GetNetwork>(_networkRepository);
        NetworkEndpointTestHelper.InitializeNetworkMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNetwork_WhenFound()
    {
        var network = NetworkTestData.Create("prod");
        network.Stacks = new List<Stack>
        {
            new CustomStack { StackName = "a", DockerProjectName = "stack-a", NetworkName = "default" }
        };

        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(network);

        await _endpoint.HandleAsync(new GetNetworkRequest { Name = "prod" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("prod", _endpoint.Response.Name);
        Assert.Single(_endpoint.Response.Stacks);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenMissing()
    {
        _networkRepository.GetByNameAsync("missing", Arg.Any<CancellationToken>())
            .Returns((Network?)null);

        await _endpoint.HandleAsync(new GetNetworkRequest { Name = "missing" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }
}
