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
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Domain.Repositories;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class GetAllStacksEndpointTests
{
    private readonly IStackRepository _stackRepository;
    private readonly IDockerProjectService _dockerProjectService;
    private readonly GetAllStacks _endpoint;

    public GetAllStacksEndpointTests()
    {
        _stackRepository = Substitute.For<IStackRepository>();
        _dockerProjectService = Substitute.For<IDockerProjectService>();
        _endpoint = FastEndpoints.Factory.Create<GetAllStacks>(_stackRepository, _dockerProjectService);
        EndpointTestHelper.InitializeStackMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsManagedAndExternalStacks()
    {
        var managed = StackTestData.Managed();
        managed.DockerProjectName = "managed";

        _stackRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns([managed]);

        _dockerProjectService.GetProjectStatusAsync("managed", Arg.Any<CancellationToken>())
            .Returns(StackStatus.Running);
        _dockerProjectService.GetProjectStatusAsync("external", Arg.Any<CancellationToken>())
            .Returns(StackStatus.Partial);

        _dockerProjectService.ListComposeProjectNamesAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal) { "managed", "external" });

        await _endpoint.HandleAsync(default);

        var result = _endpoint.Response.ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.DockerProjectName == "managed" && x.Status == StackStatus.Running);
        Assert.Contains(result, x => x.DockerProjectName == "external" && x.Source == StackSource.External);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyManaged_WhenNoExternalProjectsExist()
    {
        var managed = StackTestData.Managed();
        managed.DockerProjectName = "managed";

        _stackRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([managed]);
        _dockerProjectService.GetProjectStatusAsync("managed", Arg.Any<CancellationToken>())
            .Returns(StackStatus.Stopped);
        _dockerProjectService.ListComposeProjectNamesAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal) { "managed" });

        await _endpoint.HandleAsync(default);

        var result = _endpoint.Response.ToList();
        Assert.Single(result);
        Assert.Equal("managed", result[0].DockerProjectName);
    }
}
