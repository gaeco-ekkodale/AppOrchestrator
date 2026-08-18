// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.Stacks.Containers;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Routing;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks.Containers;

public class ListStackContainersEndpointTests
{
    private readonly IDockerContainerService _dockerContainerService;
    private readonly ListStackContainersEndpoint _endpoint;

    public ListStackContainersEndpointTests()
    {
        _dockerContainerService = Substitute.For<IDockerContainerService>();
        _endpoint = FastEndpoints.Factory.Create<ListStackContainersEndpoint>(_dockerContainerService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsContainerList_WhenContainersExist()
    {
        _dockerContainerService.ListContainersAsync("stack-a", Arg.Any<CancellationToken>())
            .Returns(new List<ContainerDTO>
            {
                new() { Id = "abc", Name = "stack-a-api", State = "running" },
                new() { Id = "def", Name = "stack-a-db", State = "exited" }
            });

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "stack-a" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(2, _endpoint.Response.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoContainersExist()
    {
        _dockerContainerService.ListContainersAsync("stack-a", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ContainerDTO>());

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "stack-a" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(_endpoint.Response);
    }
}
