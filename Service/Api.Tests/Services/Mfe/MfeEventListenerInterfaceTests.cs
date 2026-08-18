// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Extensions;
using AppOrchestrator.Api.Core.Options;
using AppOrchestrator.Api.Services._Interfaces.Mfe;
using AppOrchestrator.Api.Services.Mfe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppOrchestrator.Api.Tests.Services.Mfe;

public class MfeEventListenerInterfaceTests
{
    [Fact]
    public void MfeEventListener_Implements_IMfeEventListener()
    {
        var serviceType = typeof(DockerEventListener);
        var interfaceType = typeof(IDockerEventListener);

        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void ConfigureServices_Registers_IMfeEventListener_AsHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<OrchestratorOptions>(o => o.DockerHostUri = "npipe://./pipe/docker_engine");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Orchestrator:RootPath"] = Path.GetTempPath()
            })
            .Build();

        services.ConfigureServices(config);

        using var provider = services.BuildServiceProvider();

        var listener = provider.GetService<IDockerEventListener>();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.NotNull(listener);
        Assert.Contains(hostedServices, hs => ReferenceEquals(hs, listener));
    }
}

