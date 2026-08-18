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
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.ContainerRegistries;

public class TestContainerRegistryEndpointTests
{
    private readonly IDockerRegistryService _dockerRegistryService;
    private readonly TestContainerRegistry _endpoint;

    public TestContainerRegistryEndpointTests()
    {
        _dockerRegistryService = Substitute.For<IDockerRegistryService>();
        _endpoint = FastEndpoints.Factory.Create<TestContainerRegistry>(_dockerRegistryService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccessResponse_WhenProbeSucceeds()
    {
        _dockerRegistryService.TestRegistryAsync("ghcr.io", "user", "secret", Arg.Any<CancellationToken>())
            .Returns((true, "ok"));

        await _endpoint.HandleAsync(new TestContainerRegistryRequest
        {
            ServerAddress = "ghcr.io",
            Username = "user",
            Password = "secret"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.True(_endpoint.Response.Success);
        Assert.Equal("Login successful.", _endpoint.Response.Message);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailureMessage_WhenProbeFails()
    {
        _dockerRegistryService.TestRegistryAsync("ghcr.io", "user", "bad", Arg.Any<CancellationToken>())
            .Returns((false, "unauthorized"));

        await _endpoint.HandleAsync(new TestContainerRegistryRequest
        {
            ServerAddress = "ghcr.io",
            Username = "user",
            Password = "bad"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.False(_endpoint.Response.Success);
        Assert.Equal("unauthorized", _endpoint.Response.Message);
    }
}
