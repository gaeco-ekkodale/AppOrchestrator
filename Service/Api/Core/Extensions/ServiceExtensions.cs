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
using AppOrchestrator.Api.Services._Interfaces;
using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Services._Interfaces.Mfe;
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Services.Docker;
using AppOrchestrator.Api.Services;
using AppOrchestrator.Api.Services.Mfe;
using AppOrchestrator.Api.Services.Stacks;
using AppOrchestrator.Api.Services.Storage;
using AppOrchestrator.Infrastructure;
using Docker.DotNet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Core.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configures and registers application services in the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Data Protection for encrypting registry API keys.
        // Keys are persisted relative to the configured root path so they survive container restarts.
        var rootPath = configuration["Orchestrator:RootPath"] ?? "/orchestrator";
        var dpKeysPath = Path.Combine(rootPath, "dp-keys");
        services.AddDataProtection()
            .SetApplicationName("AppOrchestrator")
            .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

        services.AddSingleton<IRegistrySecretProtector, RegistrySecretProtector>();

        // Dependency injection for external services
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // HTTP client used to communicate with AppRegistry microservices
        services.AddHttpClient("RegistryClient");

        // HTTP client used to notify the MFE shell host
        services.AddHttpClient("MfeHostClient");

        // Docker client - communicates with the host Docker daemon.
        // The endpoint URI is read from Orchestrator:DockerHostUri (appsettings):
        //   Linux / Docker container : unix:///var/run/docker.sock
        //   Windows local dev        : npipe://./pipe/docker_engine
        services.AddSingleton<IDockerClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OrchestratorOptions>>().Value;
            var endpoint = new Uri(opts.DockerHostUri);
            return new DockerClientConfiguration(endpoint).CreateClient();
        });

        // Application services
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IComposeEnvironmentBuilder, ComposeEnvironmentBuilder>();
        services.AddScoped<IDockerComposeCommandRunner, DockerComposeCommandRunner>();

        // Function-group application services
        services.AddScoped<IStackDeploymentService, StackDeploymentService>();
        services.AddScoped<IDockerProjectService, DockerProjectService>();
        services.AddScoped<IDockerContainerService, DockerContainerService>();
        services.AddScoped<IDockerRegistryService, DockerRegistryService>();
        services.AddScoped<IDockerNetworkService, DockerNetworkService>();
        services.AddScoped<IAppRegistryClient, AppRegistryClient>();
        services.AddScoped<IRegistryProxyService, RegistryProxyService>();
        services.AddScoped<IStackBackupService, StackBackupService>();

        // MFE sync: dynamic host discovery + background event listener.
        services.AddSingleton<IMfeSyncService, MfeSyncService>();
        services.AddSingleton<IDockerEventListener, DockerEventListener>();
        services.AddSingleton<IHostedService>(sp => (IHostedService)sp.GetRequiredService<IDockerEventListener>());
    }
}