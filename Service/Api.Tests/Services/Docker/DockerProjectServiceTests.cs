// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Exceptions;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Services.Docker;
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Services.Docker;

public class DockerProjectServiceTests
{
    [Fact]
    public async Task StartProjectAsync_ThrowsDockerOperationException_WhenComposeFailsForManagedStack()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var stackRepo = Substitute.For<IStackRepository>();
        var commandRunner = Substitute.For<IDockerComposeCommandRunner>();
        var fileService = Substitute.For<IFileService>();
        var envBuilder = Substitute.For<IComposeEnvironmentBuilder>();

        stackRepo.GetAsync("orch-demo", Arg.Any<CancellationToken>())
            .Returns(new CustomStack { StackName = "Orch Demo", DockerProjectName = "orch-demo", NetworkName = "shared-net" });
        fileService.GetInternalWorkspacePath("orch-demo").Returns("C:\\tmp\\orch-demo");
        envBuilder.BuildAsync("orch-demo", "shared-net", Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["STACK_NAME"] = "orch-demo" });
        commandRunner.RunComposeUpAsync("C:\\tmp\\orch-demo", "orch-demo", Arg.Any<Dictionary<string, string>>(), false, Arg.Any<CancellationToken>())
            .Returns(new DockerComposeCommandResult(1, string.Empty, "compose failed"));

        var sut = new DockerProjectService(
            Substitute.For<ILogger<DockerProjectService>>(),
            dockerClient,
            commandRunner,
            envBuilder,
            stackRepo,
            fileService);

        await Assert.ThrowsAsync<DockerOperationException>(() => sut.StartProjectAsync("orch-demo", CancellationToken.None));
    }

    [Fact]
    public async Task RestartProjectAsync_ThrowsInvalidOperationException_WhenExternalProjectHasNoContainers()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        var stackRepo = Substitute.For<IStackRepository>();
        stackRepo.GetAsync("orch-external", Arg.Any<CancellationToken>()).Returns((Stack?)null);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([]));

        var sut = new DockerProjectService(
            Substitute.For<ILogger<DockerProjectService>>(),
            dockerClient,
            Substitute.For<IDockerComposeCommandRunner>(),
            Substitute.For<IComposeEnvironmentBuilder>(),
            stackRepo,
            Substitute.For<IFileService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RestartProjectAsync("orch-external", CancellationToken.None));
    }

    [Fact]
    public async Task GetProjectStatusAsync_ReturnsPartial_WhenSomeContainersAreRunning()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new() { ID = "a", State = "running" },
                new() { ID = "b", State = "exited" }
            ]));

        var sut = new DockerProjectService(
            Substitute.For<ILogger<DockerProjectService>>(),
            dockerClient,
            Substitute.For<IDockerComposeCommandRunner>(),
            Substitute.For<IComposeEnvironmentBuilder>(),
            Substitute.For<IStackRepository>(),
            Substitute.For<IFileService>());

        var status = await sut.GetProjectStatusAsync("orch-demo", CancellationToken.None);

        Assert.Equal(StackStatus.Partial, status);
    }

    [Fact]
    public async Task ListComposeProjectNamesAsync_ReturnsDistinctNonEmptyNames()
    {
        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new() { Labels = new Dictionary<string, string> { ["com.docker.compose.project"] = "orch-a" } },
                new() { Labels = new Dictionary<string, string> { ["com.docker.compose.project"] = "orch-b" } },
                new() { Labels = new Dictionary<string, string> { ["com.docker.compose.project"] = "orch-a" } },
                new() { Labels = new Dictionary<string, string>() }
            ]));

        var sut = new DockerProjectService(
            Substitute.For<ILogger<DockerProjectService>>(),
            dockerClient,
            Substitute.For<IDockerComposeCommandRunner>(),
            Substitute.For<IComposeEnvironmentBuilder>(),
            Substitute.For<IStackRepository>(),
            Substitute.For<IFileService>());

        var names = await sut.ListComposeProjectNamesAsync(CancellationToken.None);

        Assert.Equal(2, names.Count);
        Assert.Contains("orch-a", names);
        Assert.Contains("orch-b", names);
    }
}
