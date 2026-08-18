// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Options;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Core.Extensions;

/// <summary>
/// Extensions for data access related operations such as Entity Framework Core, Dapper, or other ORMs.
/// Configure your DbContexts, repository interfaces, and their implementations here.
/// </summary>
public static class DataAccessExtensions
{
    public static void AddPostgres(this IServiceCollection services)
    {
        services.AddDbContext<AppOrchestratorDbContext>((provider, builder) =>
        {
            var postgresOptions = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;

            builder.UseNpgsql(
                $"Host={postgresOptions.Host};" +
                $"Port={postgresOptions.Port};" +
                $"Database={postgresOptions.Database};" +
                $"Username={postgresOptions.User};" +
                $"Password={postgresOptions.Password}");
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    public static async Task MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppOrchestratorDbContext>();
        await dbContext.Database.MigrateAsync();
        await SeedAsync(dbContext);
    }

    private static async Task SeedAsync(AppOrchestratorDbContext dbContext)
    {
        const string gaecoLocal = "gaeco-local";

        if (!await dbContext.Networks.AnyAsync())
        {
            dbContext.Networks.Add(new Network { Name = gaecoLocal });
            await dbContext.SaveChangesAsync();
        }
    }
}