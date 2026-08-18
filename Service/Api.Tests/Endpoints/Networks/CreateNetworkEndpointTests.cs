// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.Networks;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Endpoints.Networks;

public class CreateNetworkEndpointTests
{
    private readonly INetworkRepository _networkRepository;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly CreateNetwork _endpoint;

    public CreateNetworkEndpointTests()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _dockerNetworkService = Substitute.For<IDockerNetworkService>();
        _endpoint = FastEndpoints.Factory.Create<CreateNetwork>(_networkRepository, _dockerNetworkService);
        NetworkEndpointTestHelper.InitializeNetworkMapper(_endpoint);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreated_WhenNetworkDoesNotExist()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Network?)null);

        await _endpoint.HandleAsync(new CreateNetworkRequest { Name = "prod" }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("prod", _endpoint.Response.Name);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenNetworkAlreadyExists()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.Create("prod"));

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new CreateNetworkRequest { Name = "prod" }, default));
    }

    [Fact]
    public async Task HandleAsync_PropagatesException_WhenDockerCreateFails()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Network?)null);

        _dockerNetworkService.CreateNetworkAsync("prod", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("docker failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _endpoint.HandleAsync(new CreateNetworkRequest { Name = "prod" }, default));
    }

    [Fact]
    public void Validator_RejectsNameWithInvalidCharacters()
    {
        var validator = new CreateNetworkRequestValidator();
        var result = validator.Validate(new CreateNetworkRequest { Name = "name with spaces" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task HandleAsync_PersistsAllowedVersionSuffixes_WhenProvided()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Network?)null);

        await _endpoint.HandleAsync(new CreateNetworkRequest
        {
            Name = "prod",
            AllowedVersionSuffixes = ["beta", "rc"]
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(["beta", "rc"], _endpoint.Response.AllowedVersionSuffixes);
    }

    [Fact]
    public async Task HandleAsync_DeduplicatesSuffixes_WhenDuplicatesProvided()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Network?)null);

        await _endpoint.HandleAsync(new CreateNetworkRequest
        {
            Name = "prod",
            AllowedVersionSuffixes = ["beta", "BETA", "rc"]
        }, default);

        Assert.Equal(201, _endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(2, _endpoint.Response.AllowedVersionSuffixes?.Count);
    }
}
