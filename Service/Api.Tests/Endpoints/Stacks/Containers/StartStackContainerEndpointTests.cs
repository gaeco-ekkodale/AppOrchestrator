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
using AppOrchestrator.Api.Shared.Routing;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks.Containers;

public class StartStackContainerEndpointTests
{
    private readonly IDockerContainerService _dockerContainerService;
    private readonly StartStackContainerEndpoint _endpoint;

    public StartStackContainerEndpointTests()
    {
        _dockerContainerService = Substitute.For<IDockerContainerService>();
        _endpoint = FastEndpoints.Factory.Create<StartStackContainerEndpoint>(_dockerContainerService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoContent_WhenStartSucceeds()
    {
        _dockerContainerService.StartContainerAsync("stack-a", "api", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _endpoint.HandleAsync(new StackContainerRouteParams { ProjectName = "stack-a", ContainerId = "api" }, default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PropagatesException_WhenContainerNotFound()
    {
        _dockerContainerService.StartContainerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _endpoint.HandleAsync(new StackContainerRouteParams { ProjectName = "stack-a", ContainerId = "missing" }, default));
    }
}
