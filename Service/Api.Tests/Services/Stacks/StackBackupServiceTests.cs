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
using AppOrchestrator.Api.Core.Options;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Services.Stacks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Services.Stacks;

public class StackBackupServiceTests
{
    [Fact]
    public async Task ApplyWithBackupAsync_Completes_WhenComposeUpdateSucceeds()
    {
        var fileService = Substitute.For<IFileService>();
        var commandRunner = Substitute.For<IDockerComposeCommandRunner>();
        var envBuilder = Substitute.For<IComposeEnvironmentBuilder>();

        fileService.GetInternalWorkspacePath("orch-demo").Returns("C:\\tmp\\orch-demo");
        fileService.DirectoryExists(Arg.Any<string>()).Returns(false);
        envBuilder.BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        commandRunner.RunComposeUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), false, Arg.Any<CancellationToken>())
            .Returns(new DockerComposeCommandResult(0, string.Empty, string.Empty));

        var sut = new StackBackupService(
            fileService,
            commandRunner,
            envBuilder,
            Options.Create(new OrchestratorOptions { VersionUpdateBackupRetention = 2 }),
            Substitute.For<ILogger<StackBackupService>>());

        await sut.ApplyWithBackupAsync(
            "orch-demo",
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("services:{}")),
            new Dictionary<string, string> { ["A"] = "B" },
            "shared-net",
            packageZip: null,
            CancellationToken.None);
    }

    [Fact]
    public async Task ApplyWithBackupAsync_ExtractsPackageZip_BeforeComposeUp()
    {
        var fileService = Substitute.For<IFileService>();
        var commandRunner = Substitute.For<IDockerComposeCommandRunner>();
        var envBuilder = Substitute.For<IComposeEnvironmentBuilder>();

        var workspace = "C:\\tmp\\orch-demo";
        fileService.GetInternalWorkspacePath("orch-demo").Returns(workspace);
        fileService.DirectoryExists(Arg.Any<string>()).Returns(false);
        envBuilder.BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        commandRunner.RunComposeUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), false, Arg.Any<CancellationToken>())
            .Returns(new DockerComposeCommandResult(0, string.Empty, string.Empty));

        var sut = new StackBackupService(
            fileService,
            commandRunner,
            envBuilder,
            Options.Create(new OrchestratorOptions { VersionUpdateBackupRetention = 2 }),
            Substitute.For<ILogger<StackBackupService>>());

        using var packageZip = new MemoryStream([1, 2, 3]);

        await sut.ApplyWithBackupAsync(
            "orch-demo",
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("services:{}")),
            new Dictionary<string, string> { ["A"] = "B" },
            "shared-net",
            packageZip,
            CancellationToken.None);

        Received.InOrder(() =>
        {
            fileService.ExtractPackageFilesAsync(workspace, packageZip, Arg.Any<CancellationToken>());
            commandRunner.RunComposeUpAsync(
                workspace, "orch-demo", Arg.Any<Dictionary<string, string>>(), false, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ApplyWithBackupAsync_ThrowsDockerOperationException_WhenComposeFailsEvenAfterRestore()
    {
        var fileService = Substitute.For<IFileService>();
        var commandRunner = Substitute.For<IDockerComposeCommandRunner>();
        var envBuilder = Substitute.For<IComposeEnvironmentBuilder>();

        var workspace = "C:\\tmp\\orch-demo";
        fileService.GetInternalWorkspacePath("orch-demo").Returns(workspace);
        fileService.DirectoryExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return path == workspace || path.Contains("_backup_", StringComparison.Ordinal);
        });

        envBuilder.BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        commandRunner.RunComposeUpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), false, Arg.Any<CancellationToken>())
            .Returns(
                new DockerComposeCommandResult(1, string.Empty, "first update failed"),
                new DockerComposeCommandResult(0, string.Empty, string.Empty));

        var sut = new StackBackupService(
            fileService,
            commandRunner,
            envBuilder,
            Options.Create(new OrchestratorOptions { VersionUpdateBackupRetention = 2 }),
            Substitute.For<ILogger<StackBackupService>>());

        await Assert.ThrowsAsync<DockerOperationException>(() =>
            sut.ApplyWithBackupAsync(
                "orch-demo",
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes("services:{}")),
                new Dictionary<string, string> { ["A"] = "B" },
                "shared-net",
                packageZip: null,
                CancellationToken.None));
    }
}
