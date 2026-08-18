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
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class UpdateStackComposeEndpointTests
{
    private readonly IStackDeploymentService _deploymentService;
    private readonly UpdateStackComposeEndpoint _endpoint;

    public UpdateStackComposeEndpointTests()
    {
        _deploymentService = Substitute.For<IStackDeploymentService>();
        _endpoint = FastEndpoints.Factory.Create<UpdateStackComposeEndpoint>(_deploymentService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenComposeUpdateSucceeds()
    {
        _deploymentService.UpdateComposeAsync(Arg.Any<UpdateStackComposeCommand>(), Arg.Any<CancellationToken>())
            .Returns(new StackComposeData("my-stack", "services: {}", new Dictionary<string, string> { ["A"] = "1" }));

        await _endpoint.HandleAsync(new UpdateStackComposeRequest
        {
            ProjectName = "my-stack",
            ComposeContent = "services: {}",
            EnvConfig = new Dictionary<string, string> { ["A"] = "1" }
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("my-stack", _endpoint.Response.StackName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenStackMissing()
    {
        _deploymentService.UpdateComposeAsync(Arg.Any<UpdateStackComposeCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<StackComposeData>>(_ => throw new KeyNotFoundException());

        await _endpoint.HandleAsync(new UpdateStackComposeRequest
        {
            ProjectName = "missing",
            ComposeContent = "services: {}"
        }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenComposeUpdateFails()
    {
        _deploymentService.UpdateComposeAsync(Arg.Any<UpdateStackComposeCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<StackComposeData>>(_ => throw new InvalidOperationException("docker failed"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateStackComposeRequest
            {
                ProjectName = "my-stack",
                ComposeContent = "services: {}"
            }, default));
    }
}
