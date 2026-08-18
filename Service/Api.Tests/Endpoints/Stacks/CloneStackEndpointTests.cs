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

public class CloneStackEndpointTests
{
    private readonly IStackDeploymentService _deploymentService;
    private readonly CloneStack _endpoint;

    public CloneStackEndpointTests()
    {
        _deploymentService = Substitute.For<IStackDeploymentService>();
        _endpoint = Factory.Create<CloneStack>(_deploymentService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenCloneSucceeds()
    {
        var clone = StackTestData.Managed("clone");
        _deploymentService
            .CloneAsync(
                Arg.Is<CloneStackCommand>(c => c.SourceProjectName == "source" && c.StackName == "target"),
                Arg.Any<CancellationToken>())
            .Returns(clone);

        await _endpoint.HandleAsync(new CloneStackRequest { ProjectName = "source", NewStackName = "target" }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(clone.StackName, _endpoint.Response.StackName);
    }

    [Fact]
    public async Task HandleAsync_PassesNetworkOnly_WhenNoNameGiven()
    {
        _deploymentService.CloneAsync(Arg.Any<CloneStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(StackTestData.Managed("clone"));

        await _endpoint.HandleAsync(
            new CloneStackRequest { ProjectName = "source", NetworkName = "staging" }, default);

        await _deploymentService.Received(1).CloneAsync(
            Arg.Is<CloneStackCommand>(c => c.StackName == null && c.NetworkName == "staging"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenSourceMissing()
    {
        _deploymentService.CloneAsync(Arg.Any<CloneStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new KeyNotFoundException());

        await _endpoint.HandleAsync(new CloneStackRequest { ProjectName = "source", NewStackName = "target" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenTargetAlreadyExists()
    {
        _deploymentService.CloneAsync(Arg.Any<CloneStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new InvalidOperationException("exists"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CloneStackRequest { ProjectName = "source", NewStackName = "target" }, default));
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenNeitherNameNorNetworkDiffers()
    {
        _deploymentService.CloneAsync(Arg.Any<CloneStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new ArgumentException("no change"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CloneStackRequest { ProjectName = "source", NewStackName = "source" }, default));
    }
}
