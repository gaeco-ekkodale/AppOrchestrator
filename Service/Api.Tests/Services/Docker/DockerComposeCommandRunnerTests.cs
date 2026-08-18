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
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AppOrchestrator.Api.Tests.Services.Docker;

public class DockerComposeCommandRunnerTests
{
    [Fact]
    public async Task RunComposeUpAsync_ThrowsWhenWorkingDirectoryDoesNotExist()
    {
        var sut = new DockerComposeCommandRunner(
            Substitute.For<ILogger<DockerComposeCommandRunner>>());

        var invalidDirectory = Path.Combine(Path.GetTempPath(), "does-not-exist", Guid.NewGuid().ToString("N"));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sut.RunComposeUpAsync(invalidDirectory, "orch-demo", new Dictionary<string, string>(), false, CancellationToken.None));
    }
}
