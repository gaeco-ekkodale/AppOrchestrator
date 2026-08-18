// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.Stacks;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Shared.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class StartStackEndpointTests
{
    private readonly IDockerProjectService _dockerProjectService;
    private readonly StartStack _endpoint;

    public StartStackEndpointTests()
    {
        _dockerProjectService = Substitute.For<IDockerProjectService>();
        var logger = Substitute.For<ILogger<StartStack>>();
        _endpoint = FastEndpoints.Factory.Create<StartStack>(_dockerProjectService, logger);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoContent_WhenStartSucceeds()
    {
        _dockerProjectService.StartProjectAsync("my-stack", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PropagatesException_WhenDockerFails()
    {
        _dockerProjectService.StartProjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("docker failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default));
    }

    [Fact]
    public void RouteValidator_RejectsInvalidProjectName()
    {
        var validator = new AppOrchestrator.Api.Shared.Routing.StackRouteParamsValidator();
        var result = validator.Validate(new AppOrchestrator.Api.Shared.Routing.StackRouteParams
        {
            ProjectName = "INVALID NAME"
        });

        Assert.False(result.IsValid);
    }
}
