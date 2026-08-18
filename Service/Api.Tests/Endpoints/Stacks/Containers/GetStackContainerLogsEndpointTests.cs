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
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks.Containers;

public class GetStackContainerLogsEndpointTests
{
    private readonly IDockerContainerService _dockerContainerService;
    private readonly GetStackContainerLogsEndpoint _endpoint;

    public GetStackContainerLogsEndpointTests()
    {
        _dockerContainerService = Substitute.For<IDockerContainerService>();
        _endpoint = FastEndpoints.Factory.Create<GetStackContainerLogsEndpoint>(_dockerContainerService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsLogs_WhenContainerExists()
    {
        _dockerContainerService.GetContainerLogsAsync("stack-a", "api", "2026-01-01T10:00:00Z", 300, 100, Arg.Any<CancellationToken>())
            .Returns(new ContainerLogsResponseDTO
            {
                ContainerId = "api",
                NextSince = "2026-01-01T10:00:02Z",
                Lines = new List<ContainerLogLineDTO>
                {
                    new() { Timestamp = "2026-01-01T10:00:01Z", Stream = "stdout", Message = "started" }
                }
            });

        await _endpoint.HandleAsync(new GetStackContainerLogsRequest
        {
            ProjectName = "stack-a",
            ContainerId = "api",
            Since = "2026-01-01T10:00:00Z",
            Tail = 300,
            Limit = 100
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Single(_endpoint.Response.Lines);
    }

    [Fact]
    public async Task HandleAsync_UsesZeroDefaults_WhenTailAndLimitMissing()
    {
        _dockerContainerService.GetContainerLogsAsync("stack-a", "api", null, 0, 0, Arg.Any<CancellationToken>())
            .Returns(new ContainerLogsResponseDTO { ContainerId = "api", NextSince = "", Lines = [] });

        await _endpoint.HandleAsync(new GetStackContainerLogsRequest
        {
            ProjectName = "stack-a",
            ContainerId = "api"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("api", _endpoint.Response.ContainerId);
    }

    [Fact]
    public void Validator_RejectsNegativeTail()
    {
        var validator = new GetStackContainerLogsRequestValidator();
        var result = validator.Validate(new GetStackContainerLogsRequest
        {
            ProjectName = "stack-a",
            ContainerId = "api",
            Tail = -1
        });

        Assert.False(result.IsValid);
    }
}
