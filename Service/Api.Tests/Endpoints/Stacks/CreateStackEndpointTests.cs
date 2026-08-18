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
using AppOrchestrator.Api.Tests.Endpoints.Networks;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class CreateStackEndpointTests
{
    private readonly IStackDeploymentService _deploymentService;
    private readonly IMfeSyncService _syncService;
    private readonly INetworkRepository _networkRepository;
    private readonly CreateStack _endpoint;

    public CreateStackEndpointTests()
    {
        _deploymentService = Substitute.For<IStackDeploymentService>();
        _syncService       = Substitute.For<IMfeSyncService>();
        _networkRepository = Substitute.For<INetworkRepository>();
        _endpoint = Factory.Create<CreateStack>(_deploymentService, _syncService, _networkRepository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenDeploymentSucceeds()
    {
        var created = StackTestData.Managed();
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);

        await _endpoint.HandleAsync(new CreateStackRequest
        {
            StackName   = "my-stack",
            RegistryId  = Guid.NewGuid(),
            PackageId   = "demo/pkg",
            Version     = "1.0.0",
            NetworkName = "prod"
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(created.DockerProjectName, _endpoint.Response.DockerProjectName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenRegistryDoesNotExist()
    {
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new KeyNotFoundException());

        await _endpoint.HandleAsync(new CreateStackRequest { StackName = "my-stack" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenProjectAlreadyExists()
    {
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new InvalidOperationException("exists"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateStackRequest { StackName = "my-stack" }, default));
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenRegistryIsUnavailable()
    {
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new HttpRequestException("registry unavailable"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateStackRequest { StackName = "my-stack" }, default));
    }

    [Fact]
    public async Task HandleAsync_RollsBackDeployment_WhenSyncFails()
    {
        var created = StackTestData.Managed();
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);
        _syncService.SyncAfterDeployAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new HttpRequestException("host unreachable"));

        var ex = await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateStackRequest
            {
                StackName   = "my-stack",
                RegistryId  = Guid.NewGuid(),
                PackageId   = "demo/pkg",
                Version     = "1.0.0",
                NetworkName = created.NetworkName
            }, default));

        Assert.Contains("host unreachable", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _deploymentService.Received(1).DeleteAsync(created.DockerProjectName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_RejectsEmptyStackName()
    {
        var validator = new CreateStackRequestValidator();
        var result = validator.Validate(new CreateStackRequest
        {
            StackName = string.Empty,
            PackageId = "pkg",
            Version   = "1.0.0"
        });

        Assert.False(result.IsValid);
    }

    // ── Version-check tests ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenVersionNotAllowedByNetwork()
    {
        // Network allows stable versions only (suffix = "")
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.CreateWithSuffixes("prod", ""));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateStackRequest
            {
                StackName   = "my-stack",
                RegistryId  = Guid.NewGuid(),
                PackageId   = "demo/pkg",
                Version     = "1.0.0-beta",
                NetworkName = "prod"
            }, default));
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenVersionAllowedByNetwork()
    {
        // Network allows stable versions only – "1.0.0" has no pre-release suffix
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.CreateWithSuffixes("prod", ""));

        var created = StackTestData.Managed();
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);

        await _endpoint.HandleAsync(new CreateStackRequest
        {
            StackName   = "my-stack",
            RegistryId  = Guid.NewGuid(),
            PackageId   = "demo/pkg",
            Version     = "1.0.0",
            NetworkName = "prod"
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenNetworkHasNoSuffixRestrictions()
    {
        // Network exists but places no restrictions
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.Create("prod"));

        var created = StackTestData.Managed();
        _deploymentService.CreateFromRegistryAsync(Arg.Any<CreateStackFromRegistryCommand>(), Arg.Any<CancellationToken>())
            .Returns(created);

        await _endpoint.HandleAsync(new CreateStackRequest
        {
            StackName   = "my-stack",
            RegistryId  = Guid.NewGuid(),
            PackageId   = "demo/pkg",
            Version     = "1.0.0-beta",
            NetworkName = "prod"
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
    }
}
