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
using AppOrchestrator.Api.Services._Interfaces.Mfe;
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class CreateCustomStackEndpointTests
{
    private readonly IStackDeploymentService _deploymentService;
    private readonly IMfeSyncService _syncService;
    private readonly CreateCustomStack _endpoint;

    public CreateCustomStackEndpointTests()
    {
        _deploymentService = Substitute.For<IStackDeploymentService>();
        _syncService       = Substitute.For<IMfeSyncService>();
        _endpoint = Factory.Create<CreateCustomStack>(_deploymentService, _syncService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenDeploymentSucceeds()
    {
        var created = StackTestData.Custom();
        _deploymentService.CreateCustomAsync(Arg.Any<CreateCustomStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);

        await _endpoint.HandleAsync(new CreateCustomStackRequest
        {
            StackName      = "custom-stack",
            ComposeContent = "services: {}",
            NetworkName    = "dev"
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(created.DockerProjectName, _endpoint.Response.DockerProjectName);
    }

    [Fact]
    public async Task HandleAsync_MapsNullNetworkNameToEmptyString()
    {
        var created = StackTestData.Custom();
        _deploymentService.CreateCustomAsync(Arg.Any<CreateCustomStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var command = callInfo.Arg<CreateCustomStackCommand>();
                if (command.NetworkName != string.Empty)
                    throw new InvalidOperationException("NetworkName must be empty");
                return created;
            });

        await _endpoint.HandleAsync(new CreateCustomStackRequest
        {
            StackName      = "custom-stack",
            ComposeContent = "services: {}",
            NetworkName    = null
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenProjectAlreadyExists()
    {
        _deploymentService.CreateCustomAsync(Arg.Any<CreateCustomStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new InvalidOperationException("exists"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateCustomStackRequest { StackName = "custom-stack", ComposeContent = "services: {}" }, default));
    }

    [Fact]
    public async Task HandleAsync_RollsBackDeployment_WhenSyncFails()
    {
        var created = StackTestData.Custom();
        _deploymentService.CreateCustomAsync(Arg.Any<CreateCustomStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);
        _syncService.SyncAfterDeployAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new HttpRequestException("host unreachable"));

        var ex = await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateCustomStackRequest
            {
                StackName      = "custom-stack",
                ComposeContent = "services: {}",
                NetworkName    = created.NetworkName
            }, default));

        Assert.Contains("host unreachable", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _deploymentService.Received(1).DeleteAsync(created.DockerProjectName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_RejectsEmptyComposeContent()
    {
        var validator = new CreateCustomStackRequestValidator();
        var result = validator.Validate(new CreateCustomStackRequest
        {
            StackName      = "custom",
            ComposeContent = string.Empty
        });

        Assert.False(result.IsValid);
    }
}
