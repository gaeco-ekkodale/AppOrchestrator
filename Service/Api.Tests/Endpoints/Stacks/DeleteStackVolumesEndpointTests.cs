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
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class DeleteStackVolumesEndpointTests
{
    private readonly IDockerProjectService _dockerProjectService;
    private readonly IFileService _fileService;
    private readonly DeleteStackVolumes _endpoint;

    public DeleteStackVolumesEndpointTests()
    {
        _dockerProjectService = Substitute.For<IDockerProjectService>();
        _fileService = Substitute.For<IFileService>();
        var logger = Substitute.For<ILogger<DeleteStackVolumes>>();

        _endpoint = FastEndpoints.Factory.Create<DeleteStackVolumes>(
            _dockerProjectService,
            _fileService,
            logger);
    }

    [Fact]
    public async Task HandleAsync_Returns204()
    {
        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_StopsProjectBeforeDeletingVolumes()
    {
        var callOrder = new List<string>();

        _dockerProjectService
            .When(x => x.StopProjectAsync("my-stack", Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("stop"));

        _fileService
            .When(x => x.DeleteVolumes("my-stack"))
            .Do(_ => callOrder.Add("deleteVolumes"));

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        Assert.Equal(["stop", "deleteVolumes"], callOrder);
    }

    [Fact]
    public async Task HandleAsync_CallsStopProject()
    {
        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        await _dockerProjectService.Received(1).StopProjectAsync("my-stack", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CallsDeleteVolumes()
    {
        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        await _fileService.Received(1).DeleteVolumes("my-stack");
    }
}
