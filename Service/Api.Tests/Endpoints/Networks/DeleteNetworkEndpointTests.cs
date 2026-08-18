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

public class DeleteNetworkEndpointTests
{
    private readonly INetworkRepository _networkRepository;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly DeleteNetwork _endpoint;

    public DeleteNetworkEndpointTests()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _dockerNetworkService = Substitute.For<IDockerNetworkService>();
        _endpoint = FastEndpoints.Factory.Create<DeleteNetwork>(_networkRepository, _dockerNetworkService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoContent_WhenDeleteSucceeds()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.Create("prod"));
        _dockerNetworkService.HasContainersAsync("prod", Arg.Any<CancellationToken>())
            .Returns(false);

        await _endpoint.HandleAsync(new DeleteNetworkRequest { Name = "prod" }, default);

        Assert.Equal(204, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenNetworkDoesNotExist()
    {
        _networkRepository.GetByNameAsync("missing", Arg.Any<CancellationToken>())
            .Returns((AppOrchestrator.Domain.Models.Network?)null);

        await _endpoint.HandleAsync(new DeleteNetworkRequest { Name = "missing" }, default);

        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenContainersAttached()
    {
        _networkRepository.GetByNameAsync("prod", Arg.Any<CancellationToken>())
            .Returns(NetworkTestData.Create("prod"));
        _dockerNetworkService.HasContainersAsync("prod", Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ValidationFailureException>(() =>
            _endpoint.HandleAsync(new DeleteNetworkRequest { Name = "prod" }, default));
    }
}
