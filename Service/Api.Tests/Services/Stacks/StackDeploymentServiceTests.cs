// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Services.Stacks;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Services.Stacks;

public class StackDeploymentServiceTests
{
    [Fact]
    public async Task UpdateAsync_ThrowsArgumentException_WhenNoUpdateFieldsAreProvided()
    {
        var sut = CreateSut(
            Substitute.For<IStackRepository>(),
            Substitute.For<IAppRegistryRepository>(),
            Substitute.For<IDockerProjectService>(),
            Substitute.For<IAppRegistryClient>(),
            Substitute.For<IFileService>(),
            Substitute.For<IDockerComposeCommandRunner>(),
            Substitute.For<IStackBackupService>());

        var command = new UpdateStackCommand("orch-demo", null, null, null, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCustomAsync_ThrowsInvalidOperationException_WhenProjectAlreadyExists()
    {
        var stackRepo = Substitute.For<IStackRepository>();
        stackRepo.GetAsync("orch-default-my-stack", Arg.Any<CancellationToken>())
            .Returns(new CustomStack { StackName = "My Stack", DockerProjectName = "orch-default-my-stack", NetworkName = "default" });

        var sut = CreateSut(
            stackRepo,
            Substitute.For<IAppRegistryRepository>(),
            Substitute.For<IDockerProjectService>(),
            Substitute.For<IAppRegistryClient>(),
            Substitute.For<IFileService>(),
            Substitute.For<IDockerComposeCommandRunner>(),
            Substitute.For<IStackBackupService>());

        var command = new CreateCustomStackCommand(
            "My Stack",
            "services:{}",
            new Dictionary<string, string>(),
            "default");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateCustomAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComposeAsync_ThrowsArgumentException_WhenStackIsRegistryManaged()
    {
        var stackRepo = Substitute.For<IStackRepository>();
        stackRepo.GetAsync("orch-demo", Arg.Any<CancellationToken>())
            .Returns(new RegistryStack
            {
                DockerProjectName = "orch-demo",
                StackName = "Demo",
                NetworkName = "default",
                AppRegistryId = Guid.NewGuid(),
                PackageId = "pkg",
                PackageVersion = "1.0.0"
            });

        var sut = CreateSut(
            stackRepo,
            Substitute.For<IAppRegistryRepository>(),
            Substitute.For<IDockerProjectService>(),
            Substitute.For<IAppRegistryClient>(),
            Substitute.For<IFileService>(),
            Substitute.For<IDockerComposeCommandRunner>(),
            Substitute.For<IStackBackupService>());

        var command = new UpdateStackComposeCommand("orch-demo", "services:{}", new Dictionary<string, string>());

        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateComposeAsync(command, CancellationToken.None));
    }

    //[Fact]
    //public async Task UpdateAsync_ClearsNetworkAssignment_WhenNetworkNameIsEmptyString()
    //{
    //    var stack = new Stack
    //    {
    //        StackName = "Demo",
    //        DockerProjectName = "orch-demo",
    //        PackageId = "pkg",
    //        PackageVersion = "1.0.0",
    //        NetworkName = "shared-net"
    //    };

    //    var stackRepo = Substitute.For<IStackRepository>();
    //    stackRepo.GetAsync("orch-demo", Arg.Any<CancellationToken>()).Returns(stack);

    //    var fileService = Substitute.For<IFileService>();
    //    fileService.GetWorkspacePath("orch-demo").Returns("C:\\tmp\\orch-demo");
    //    fileService.ReadEnvFileAsync("C:\\tmp\\orch-demo", Arg.Any<CancellationToken>())
    //        .Returns(new Dictionary<string, string> { ["A"] = "B" });

    //    var commandRunner = Substitute.For<IDockerComposeCommandRunner>();
    //    commandRunner.RunComposeUpAsync("C:\\tmp\\orch-demo", "orch-demo", "", false, Arg.Any<CancellationToken>())
    //        .Returns(new DockerComposeCommandResult(0, string.Empty, string.Empty));

    //    var sut = CreateSut(
    //        stackRepo,
    //        Substitute.For<IAppRegistryRepository>(),
    //        Substitute.For<IDockerProjectService>(),
    //        Substitute.For<IAppRegistryClient>(),
    //        fileService,
    //        commandRunner,
    //        Substitute.For<IStackBackupService>());

    //    var updated = await sut.UpdateAsync(
    //        new UpdateStackCommand("orch-demo", null, null, null, string.Empty),
    //        CancellationToken.None);

    //    Assert.Null(updated.NetworkName);
    //}

    private static StackDeploymentService CreateSut(
        IStackRepository stackRepo,
        IAppRegistryRepository appRegistryRepo,
        IDockerProjectService dockerProjectService,
        IAppRegistryClient appRegistryClient,
        IFileService fileService,
        IDockerComposeCommandRunner commandRunner,
        IStackBackupService backupService,
        IComposeEnvironmentBuilder? envBuilder = null)
        => new(
            stackRepo,
            appRegistryRepo,
            dockerProjectService,
            appRegistryClient,
            fileService,
            commandRunner,
            envBuilder ?? Substitute.For<IComposeEnvironmentBuilder>(),
            backupService,
            Substitute.For<ILogger<StackDeploymentService>>());
}
