// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Repositories;
using AppOrchestrator.Infrastructure.Repositories;

namespace AppOrchestrator.Api.Core.Extensions;

/// <summary>
/// Extension methods for configuring repositories.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Configures and registers repositories in the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAppRegistryRepository, AppRegistryRepository>();
        services.AddScoped<IStackRepository, StackRepository>();
        services.AddScoped<IContainerRegistryRepository, ContainerRegistryRepository>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
    }
}