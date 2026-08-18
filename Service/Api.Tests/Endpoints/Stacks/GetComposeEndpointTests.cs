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
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.Routing;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class GetComposeEndpointTests
{
    private readonly IStackRepository _stackRepository;
    private readonly IFileService _fileService;
    private readonly GetCompose _endpoint;

    public GetComposeEndpointTests()
    {
        _stackRepository = Substitute.For<IStackRepository>();
        _fileService = Substitute.For<IFileService>();
        _endpoint = FastEndpoints.Factory.Create<GetCompose>(_stackRepository, _fileService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCompose_WhenCustomStackExists()
    {
        var stack = StackTestData.Custom("Custom");
        stack.DockerProjectName = "custom";
        _stackRepository.GetAsync("custom", Arg.Any<CancellationToken>()).Returns(stack);

        _fileService.GetInternalWorkspacePath("custom").Returns("c:/work/custom");
        _fileService.ReadComposeFileAsync("c:/work/custom", Arg.Any<CancellationToken>())
            .Returns("services: {}");
        _fileService.ReadEnvFileAsync("c:/work/custom", Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["X"] = "1" });

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "custom" }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("Custom", _endpoint.Response.StackName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenStackMissing()
    {
        _stackRepository.GetAsync("missing", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Stack?)null);

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "missing" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenStackIsRegistryManaged()
    {
        var stack = StackTestData.Managed();
        stack.DockerProjectName = "managed";
        _stackRepository.GetAsync("managed", Arg.Any<CancellationToken>()).Returns(stack);

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new StackRouteParams { ProjectName = "managed" }, default));
    }
}
