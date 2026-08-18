// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Services.Docker;

public class DockerRegistryServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WhenAuthenticationSucceeds()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var systemOperations = Substitute.For<ISystemOperations>();
        dockerClient.System.Returns(systemOperations);
        systemOperations.AuthenticateAsync(Arg.Any<AuthConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DockerRegistryService(Substitute.For<ILogger<DockerRegistryService>>(), dockerClient);

        var result = await sut.LoginAsync("myregistry.azurecr.io", "user", "pass", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Login Succeeded", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenUnexpectedExceptionOccurs()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var systemOperations = Substitute.For<ISystemOperations>();
        dockerClient.System.Returns(systemOperations);
        systemOperations.AuthenticateAsync(Arg.Any<AuthConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("auth failed")));

        var sut = new DockerRegistryService(Substitute.For<ILogger<DockerRegistryService>>(), dockerClient);

        var result = await sut.LoginAsync("myregistry.azurecr.io", "user", "pass", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("auth failed", result.Message);
    }

    [Fact]
    public async Task TestRegistryAsync_ReturnsNormalizedSuccessMessage_WhenLoginSucceeds()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var systemOperations = Substitute.For<ISystemOperations>();
        dockerClient.System.Returns(systemOperations);
        systemOperations.AuthenticateAsync(Arg.Any<AuthConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DockerRegistryService(Substitute.For<ILogger<DockerRegistryService>>(), dockerClient);

        var result = await sut.TestRegistryAsync("myregistry.azurecr.io", "user", "pass", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Login successful.", result.Message);
    }
}
