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

namespace AppOrchestrator.Api.Tests.Endpoints.Networks;

internal static class NetworkTestData
{
    public static Network Create(string name = "prod")
    {
        return new Network
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Stacks = []
        };
    }

    public static Network CreateWithSuffixes(string name, params string[] suffixes)
    {
        var network = Create(name);
        network.AllowedVersionSuffixes = suffixes
            .Select(s => new AllowedVersionSuffix { Suffix = s })
            .ToList();
        return network;
    }
}
