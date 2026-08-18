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

public class GetStackContainerEndpointTests
{
    private readonly IDockerContainerService _dockerContainerService;
    private readonly GetStackContainerEndpoint _endpoint;

    public GetStackContainerEndpointTests()
    {
        _dockerContainerService = Substitute.For<IDockerContainerService>();
        _endpoint = FastEndpoints.Factory.Create<GetStackContainerEndpoint>(_dockerContainerService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsContainer_WhenMatchExists()
    {
        _dockerContainerService.GetContainerAsync("stack-a", "api", Arg.Any<CancellationToken>())
            .Returns(new ContainerDTO { Id = "abc", Name = "stack-a-api", State = "running" });

        await _endpoint.HandleAsync(new StackContainerRouteParams { ProjectName = "stack-a", ContainerId = "api" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("stack-a-api", _endpoint.Response.Name);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenContainerMissing()
    {
        _dockerContainerService.GetContainerAsync("stack-a", "missing", Arg.Any<CancellationToken>())
            .Returns((ContainerDTO?)null);

        await _endpoint.HandleAsync(new StackContainerRouteParams { ProjectName = "stack-a", ContainerId = "missing" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }
}
