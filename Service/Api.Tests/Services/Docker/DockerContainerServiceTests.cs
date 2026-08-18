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

public class DockerContainerServiceTests
{
    [Fact]
    public async Task ListContainersAsync_MapsContainerData()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "1234567890abcdef",
                    Names = ["/svc-api"],
                    Image = "my-image:1.0",
                    State = "running",
                    Status = "Up 1 minute",
                    Labels = new Dictionary<string, string>
                    {
                        ["com.docker.compose.service"] = "api"
                    },
                    Ports =
                    [
                        new Port { IP = "0.0.0.0", PublicPort = 8080, PrivatePort = 80, Type = "tcp" }
                    ]
                }
            ]));

        var sut = new DockerContainerService(Substitute.For<ILogger<DockerContainerService>>(), dockerClient);

        var result = await sut.ListContainersAsync("orch-demo", CancellationToken.None);

        Assert.Single(result);
        var item = result[0];
        Assert.Equal("1234567890ab", item.Id);
        Assert.Equal("svc-api", item.Name);
        Assert.Equal("api", item.Service);
        Assert.Equal("running", item.State);
        Assert.Contains("0.0.0.0:8080->80/tcp", item.Ports);
    }

    [Fact]
    public async Task GetContainerAsync_ReturnsByName()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "abcdef1234567890",
                    Names = ["/svc-api"],
                    Labels = new Dictionary<string, string>(),
                    Ports = []
                }
            ]));

        var sut = new DockerContainerService(Substitute.For<ILogger<DockerContainerService>>(), dockerClient);

        var result = await sut.GetContainerAsync("orch-demo", "svc-api", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("svc-api", result!.Name);
    }

    [Fact]
    public async Task StartContainerAsync_ThrowsKeyNotFoundException_WhenContainerNotFound()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([]));

        var sut = new DockerContainerService(Substitute.For<ILogger<DockerContainerService>>(), dockerClient);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.StartContainerAsync("orch-demo", "missing", CancellationToken.None));
    }

    [Fact]
    public async Task GetContainerLogsAsync_ThrowsKeyNotFoundException_WhenContainerNotFound()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([]));

        var sut = new DockerContainerService(Substitute.For<ILogger<DockerContainerService>>(), dockerClient);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetContainerLogsAsync("orch-demo", "missing", null, 0, 0, CancellationToken.None));
    }

    [Fact]
    public async Task RestartContainerAsync_ThrowsKeyNotFoundException_WhenContainerNotFound()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([]));

        var sut = new DockerContainerService(Substitute.For<ILogger<DockerContainerService>>(), dockerClient);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.RestartContainerAsync("orch-demo", "missing", CancellationToken.None));
    }
}
