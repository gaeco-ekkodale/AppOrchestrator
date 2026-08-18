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

namespace AppOrchestrator.Api.Tests.Endpoints.AppRegistries;

internal static class AppRegistryTestData
{
    public static AppRegistry Create(string name = "Default Registry", string baseUrl = "https://registry.example")
    {
        return new AppRegistry
        {
            Id = Guid.NewGuid(),
            Name = name,
            BaseUrl = baseUrl,
            CreatedAt = DateTime.UtcNow,
            Stacks = []
        };
    }
}
