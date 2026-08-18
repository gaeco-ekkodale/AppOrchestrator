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
using AppOrchestrator.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class DeleteStackEndpointTests
{
    private readonly IDockerProjectService _dockerProjectService;
    private readonly IFileService _fileService;
    private readonly IStackRepository _stackRepository;
    private readonly DeleteStack _endpoint;

    public DeleteStackEndpointTests()
    {
        _dockerProjectService = Substitute.For<IDockerProjectService>();
        _fileService = Substitute.For<IFileService>();
        _stackRepository = Substitute.For<IStackRepository>();
        var logger = Substitute.For<ILogger<DeleteStack>>();

        _endpoint = FastEndpoints.Factory.Create<DeleteStack>(
            _dockerProjectService,
            _fileService,
            _stackRepository,
            logger);
    }

    [Fact]
    public async Task HandleAsync_DeletesWorkspace_WhenDirectoryExists()
    {
        _fileService.GetInternalWorkspacePath("my-stack").Returns("c:/work/my-stack");
        _fileService.DirectoryExists("c:/work/my-stack").Returns(true);

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_DoesNotDeleteWorkspace_WhenDirectoryMissing()
    {
        _fileService.GetInternalWorkspacePath("my-stack").Returns("c:/work/my-stack");
        _fileService.DirectoryExists("c:/work/my-stack").Returns(false);
        _fileService.DeleteDirectory(Arg.Any<string>());
        _fileService.When(x => x.DeleteDirectory(Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("Delete should not be called"));

        await _endpoint.HandleAsync(new StackRouteParams { ProjectName = "my-stack" }, default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }
}
