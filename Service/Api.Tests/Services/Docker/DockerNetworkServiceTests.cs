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

public class DockerNetworkServiceTests
{
    [Fact]
    public async Task CreateNetworkAsync_Completes_WhenDockerAcceptsRequest()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var networkOps = Substitute.For<INetworkOperations>();
        dockerClient.Networks.Returns(networkOps);

        networkOps.CreateNetworkAsync(Arg.Any<NetworksCreateParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NetworksCreateResponse { ID = "net-id" }));

        var sut = new DockerNetworkService(Substitute.For<ILogger<DockerNetworkService>>(), dockerClient);

        await sut.CreateNetworkAsync("shared-net", CancellationToken.None);
    }

    [Fact]
    public async Task DeleteNetworkAsync_Completes_WhenDockerAcceptsRequest()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var networkOps = Substitute.For<INetworkOperations>();
        dockerClient.Networks.Returns(networkOps);

        networkOps.DeleteNetworkAsync("shared-net", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DockerNetworkService(Substitute.For<ILogger<DockerNetworkService>>(), dockerClient);

        await sut.DeleteNetworkAsync("shared-net", CancellationToken.None);
    }

    [Fact]
    public async Task HasContainersAsync_ReturnsTrue_WhenInspectContainsAttachedContainers()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var networkOps = Substitute.For<INetworkOperations>();
        dockerClient.Networks.Returns(networkOps);

        networkOps.InspectNetworkAsync("shared-net", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NetworkResponse
            {
                Containers = new Dictionary<string, EndpointResource> { ["container-1"] = new() }
            }));

        var sut = new DockerNetworkService(Substitute.For<ILogger<DockerNetworkService>>(), dockerClient);

        var hasContainers = await sut.HasContainersAsync("shared-net", CancellationToken.None);

        Assert.True(hasContainers);
    }

    [Fact]
    public async Task HasContainersAsync_ReturnsFalse_WhenInspectContainsNoContainers()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var networkOps = Substitute.For<INetworkOperations>();
        dockerClient.Networks.Returns(networkOps);

        networkOps.InspectNetworkAsync("shared-net", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NetworkResponse { Containers = new Dictionary<string, EndpointResource>() }));

        var sut = new DockerNetworkService(Substitute.For<ILogger<DockerNetworkService>>(), dockerClient);

        var hasContainers = await sut.HasContainersAsync("shared-net", CancellationToken.None);

        Assert.False(hasContainers);
    }
}
