// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Core.Exceptions;

/// <summary>
/// Thrown when a Docker CLI or Docker Engine API operation fails.
/// <see cref="DockerOutput"/> contains the raw stderr / error response from Docker,
/// allowing clients to see exactly what went wrong.
/// </summary>
public class DockerOperationException : Exception
{
    public string ProjectName { get; }
    public string Operation { get; }
    /// <summary>Raw stderr captured from the Docker CLI or Docker Engine API error response.</summary>
    public string DockerOutput { get; }

    public DockerOperationException(string projectName, string operation, string dockerOutput)
        : base($"Docker '{operation}' failed for project '{projectName}': {dockerOutput}")
    {
        ProjectName = projectName;
        Operation = operation;
        DockerOutput = dockerOutput;
    }
}
