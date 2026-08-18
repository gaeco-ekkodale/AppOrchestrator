// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Models;

namespace AppOrchestrator.Api.Tests.Endpoints.Stacks;

internal static class StackTestData
{
    public static RegistryStack Managed(string name = "My Stack")
    {
        return new RegistryStack
        {
            Id = Guid.NewGuid(),
            StackName = name,
            DockerProjectName = "my-stack",
            AppRegistryId = Guid.NewGuid(),
            PackageId = "demo/pkg",
            PackageVersion = "1.0.0",
            NetworkName = "prod",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static CustomStack Custom(string name = "My Custom Stack")
    {
        return new CustomStack
        {
            Id = Guid.NewGuid(),
            StackName = name,
            DockerProjectName = "my-custom-stack",
            NetworkName = "dev",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
