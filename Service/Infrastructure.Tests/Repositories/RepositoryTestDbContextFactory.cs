// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;

namespace AppOrchestrator.Infrastructure.Tests.Repositories;

internal static class RepositoryTestDbContextFactory
{
    public static AppOrchestratorDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppOrchestratorDbContext>()
            .UseInMemoryDatabase($"infra-tests-{Guid.NewGuid()}")
            .Options;

        return new AppOrchestratorDbContext(options);
    }
}
