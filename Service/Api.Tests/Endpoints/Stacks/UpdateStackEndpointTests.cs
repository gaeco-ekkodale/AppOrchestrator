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
using AppOrchestrator.Api.Tests.Endpoints.Networks;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

public class UpdateStackEndpointTests
{
    private readonly IStackDeploymentService _deploymentService;
    private readonly INetworkRepository _networkRepository;
    private readonly IStackRepository _stackRepository;
    private readonly UpdateStackEndpoint _endpoint;

    public UpdateStackEndpointTests()
    {
        _deploymentService = Substitute.For<IStackDeploymentService>();
        _networkRepository = Substitute.For<INetworkRepository>();
        _stackRepository   = Substitute.For<IStackRepository>();
        _endpoint = FastEndpoints.Factory.Create<UpdateStackEndpoint>(_deploymentService, _networkRepository, _stackRepository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenUpdateSucceeds()
    {
        var existing = StackTestData.Managed();
        var updated  = StackTestData.Managed("Updated");

        // Version is set -> endpoint looks up the current stack first
        _stackRepository.GetAsync("my-stack", Arg.Any<CancellationToken>())
            .Returns(existing);
        // Network has no suffix restrictions -> all versions allowed
        _networkRepository.GetByNameAsync(existing.NetworkName!, Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.Create(existing.NetworkName!));
        _deploymentService.UpdateAsync(Arg.Any<UpdateStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(updated);

        await _endpoint.HandleAsync(new UpdateStackRequest
        {
            ProjectName = "my-stack",
            StackName   = "updated",
            Version     = "2.0.0",
            EnvConfig   = new Dictionary<string, string> { ["A"] = "1" }
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(updated.StackName, _endpoint.Response.StackName);
    }

    [Fact]
    public async Task HandleAsync_MapsNullNetworkNameToEmptyString()
    {
        var updated = StackTestData.Managed();
        _deploymentService.UpdateAsync(Arg.Any<UpdateStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var command = callInfo.Arg<UpdateStackCommand>();
                if (command.NetworkName != string.Empty)
                    throw new InvalidOperationException("NetworkName must be empty");
                return updated;
            });

        // No Version -> version check is skipped
        await _endpoint.HandleAsync(new UpdateStackRequest
        {
            ProjectName = "my-stack",
            NetworkName = ""
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenStackMissing()
    {
        _deploymentService.UpdateAsync(Arg.Any<UpdateStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new KeyNotFoundException());

        // No Version -> version check is skipped, KeyNotFoundException from deploymentService -> 404
        await _endpoint.HandleAsync(new UpdateStackRequest { ProjectName = "missing" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenPayloadInvalidForUpdate()
    {
        _deploymentService.UpdateAsync(Arg.Any<UpdateStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new ArgumentException("invalid"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateStackRequest { ProjectName = "my-stack" }, default));
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenRenameConflictOccurs()
    {
        _deploymentService.UpdateAsync(Arg.Any<UpdateStackCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<AppOrchestrator.Domain.Models.Stack>>(_ => throw new InvalidOperationException("conflict"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateStackRequest { ProjectName = "my-stack", StackName = "other" }, default));
    }

    [Fact]
    public void Validator_RejectsTooLongVersion()
    {
        var validator = new UpdateStackRequestValidator();
        var result = validator.Validate(new UpdateStackRequest
        {
            ProjectName = "my-stack",
            Version     = new string('x', 101)
        });

        Assert.False(result.IsValid);
    }

    // ── Version-check tests ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenVersionNotAllowedByCurrentNetwork()
    {
        // Stack lives in "prod" which allows stable versions only
        var existing = StackTestData.Managed(); // NetworkName = "prod"
        _stackRepository.GetAsync("my-stack", Arg.Any<CancellationToken>())
            .Returns(existing);
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.CreateWithSuffixes("prod", ""));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateStackRequest
            {
                ProjectName = "my-stack",
                Version     = "2.0.0-beta"
            }, default));
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenVersionAllowedByCurrentNetwork()
    {
        var existing = StackTestData.Managed(); // NetworkName = "prod"
        var updated  = StackTestData.Managed("Updated");

        _stackRepository.GetAsync("my-stack", Arg.Any<CancellationToken>())
            .Returns(existing);
        // stable-only network, "2.0.0" has no pre-release suffix -> allowed
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.CreateWithSuffixes("prod", ""));
        _deploymentService.UpdateAsync(Arg.Any<UpdateStackCommand>(), Arg.Any<CancellationToken>())
            .Returns(updated);

        await _endpoint.HandleAsync(new UpdateStackRequest
        {
            ProjectName = "my-stack",
            Version     = "2.0.0"
        }, default);

        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UsesNewNetworkWhenCheckingVersion()
    {
        // Stack is currently on "dev" (no restrictions), but being moved to "prod" (stable only)
        var existing = StackTestData.Managed();
        existing.NetworkName = "dev";

        _stackRepository.GetAsync("my-stack", Arg.Any<CancellationToken>())
            .Returns(existing);
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.CreateWithSuffixes("prod", ""));

        // "1.0.0-beta" is not allowed in "prod" -> should throw
        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new UpdateStackRequest
            {
                ProjectName = "my-stack",
                Version     = "1.0.0-beta",
                NetworkName = "prod"
            }, default));
    }
}
