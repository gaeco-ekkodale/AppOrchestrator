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
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Routing;
using AppOrchestrator.Domain.Repositories;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class GetStackEndpointTests
{
    private readonly IStackRepository _stackRepository;
    private readonly IDockerProjectService _dockerProjectService;
    private readonly IFileService _fileService;
    private readonly GetStack _endpoint;

    public GetStackEndpointTests()
    {
        _stackRepository = Substitute.For<IStackRepository>();
        _dockerProjectService = Substitute.For<IDockerProjectService>();
        _fileService = Substitute.For<IFileService>();
        _endpoint = FastEndpoints.Factory.Create<GetStack>(_stackRepository, _dockerProjectService, _fileService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsManagedStack_WhenPersistedStackExists()
    {
        var stack = StackTestData.Managed();
        stack.DockerProjectName = "managed";

        _stackRepository.GetAsync("managed", Arg.Any<CancellationToken>()).Returns(stack);
        _dockerProjectService.GetProjectStatusAsync("managed", Arg.Any<CancellationToken>())
            .Returns(StackStatus.Running);

        _fileService.GetInternalWorkspacePath("managed").Returns("c:/work/managed");
        _fileService.ReadEnvFileAsync("c:/work/managed", Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["A"] = "1" });

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "managed" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(StackStatus.Running, _endpoint.Response.Status);
        Assert.Equal("1", _endpoint.Response.EnvConfig!["A"]);
    }

    [Fact]
    public async Task HandleAsync_ReturnsExternalStack_WhenOnlyDockerProjectExists()
    {
        _stackRepository.GetAsync("external", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Stack?)null);

        _dockerProjectService.ListComposeProjectNamesAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal) { "external" });

        _dockerProjectService.GetProjectStatusAsync("external", Arg.Any<CancellationToken>())
            .Returns(StackStatus.Stopped);

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "external" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(StackSource.External, _endpoint.Response.Source);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenNeitherManagedNorDockerProjectExists()
    {
        _stackRepository.GetAsync("missing", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Stack?)null);

        _dockerProjectService.ListComposeProjectNamesAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal));

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "missing" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }
}
