// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services.Mfe;
using AppOrchestrator.Api.Services._Interfaces.Mfe;
using AppOrchestrator.Api.Tests.Services.Http;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace AppOrchestrator.Api.Tests.Services.Mfe;

public class MfeSyncServiceTests
{
    [Fact]
    public async Task SyncNetworkAsync_PostsSnapshotDtoForTargetNetwork()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("MfeHostClient").Returns(new HttpClient(handler));

        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        // One host container and one plugin container in target network.
        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "host-1",
                    State = "running",
                    Names = ["/mfe-host"],
                    Labels = new Dictionary<string, string>
                    {
                        [MfeLabels.HostEnabled] = "true",
                        [MfeLabels.HostApiKey] = "test-api-key"
                    },
                    Ports = [new Port { Type = "tcp", PrivatePort = 8080 }],
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                },
                new()
                {
                    ID = "plugin-1",
                    State = "running",
                    Names = ["/todo-ui"],
                    Labels = new Dictionary<string, string>
                    {
                        ["app.mfe.id"] = "todo-app",
                        ["app.mfe.entrypointPath"] = "assets/remoteEntry.js",
                        ["app.mfe.displayName"] = "Todo",
                        ["app.mfe.exposedModule"] = "./Module",
                        ["app.mfe.route"] = "/todo",
                        ["app.mfe.customMeta"] = "v1"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                },
                new()
                {
                    ID = "plugin-2",
                    State = "exited",
                    Names = ["/other-ui"],
                    Labels = new Dictionary<string, string>
                    {
                        ["app.mfe.entrypointPath"] = "assets/remoteEntry.js",
                        ["app.mfe.displayName"] = "Other"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["other-network"] = new()
                        }
                    }
                }
            ]));

        var sut = new MfeSyncService(
            dockerClient,
            clientFactory,
            Substitute.For<ILogger<MfeSyncService>>());

        await sut.SyncNetworkAsync("my-network", CancellationToken.None);

        Assert.Single(handler.Requests);
        Assert.Equal("http://mfe-host:8080/api/plugins", handler.Requests[0].RequestUri!.ToString());

        var json = await handler.Requests[0].Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var plugins = doc.RootElement.GetProperty("plugins");
        Assert.Equal(1, plugins.GetArrayLength());

        var first = plugins[0];
        Assert.Equal("todo-app", first.GetProperty("id").GetString());
        Assert.Equal("Todo", first.GetProperty("displayName").GetString());
        Assert.Equal("http://todo-ui", first.GetProperty("containerBaseUrl").GetString());
        Assert.Equal("assets/remoteEntry.js", first.GetProperty("entrypointPath").GetString());
        Assert.Equal("./Module", first.GetProperty("exposedModule").GetString());
        Assert.Equal("/todo", first.GetProperty("route").GetString());
        Assert.Equal("running", first.GetProperty("state").GetString());
        Assert.False(first.TryGetProperty("labels", out _));
        Assert.False(first.TryGetProperty("app.mfe.customMeta", out _));
    }

    [Fact]
    public async Task SyncNetworkAsync_DoesNothing_WhenNoHostContainerExists()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("MfeHostClient").Returns(new HttpClient(handler));

        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);
        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "plugin-1",
                    State = "running",
                    Names = ["/todo-ui"],
                    Labels = new Dictionary<string, string>
                    {
                        ["app.mfe.displayName"] = "Todo"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                }
            ]));

        var sut = new MfeSyncService(
            dockerClient,
            clientFactory,
            Substitute.For<ILogger<MfeSyncService>>());

        await sut.SyncNetworkAsync("my-network", CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SyncAfterDeployAsync_Throws_WhenDeployedStackHasPlugins_AndMultipleHostsExist()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("MfeHostClient").Returns(new HttpClient(handler));

        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "host-1",
                    Names = ["/mfe-host-1"],
                    Labels = new Dictionary<string, string>
                    {
                        [MfeLabels.HostEnabled] = "true",
                        [MfeLabels.HostApiKey] = "k1"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                },
                new()
                {
                    ID = "host-2",
                    Names = ["/mfe-host-2"],
                    Labels = new Dictionary<string, string>
                    {
                        [MfeLabels.HostEnabled] = "true",
                        [MfeLabels.HostApiKey] = "k2"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                },
                new()
                {
                    ID = "plugin-1",
                    State = "running",
                    Names = ["/my-plugin"],
                    Labels = new Dictionary<string, string>
                    {
                        ["com.docker.compose.project"] = "my-stack",
                        ["app.mfe.id"] = "my-plugin",
                        ["app.mfe.entrypointPath"] = "remoteEntry.js",
                        ["app.mfe.displayName"] = "Plugin",
                        ["app.mfe.exposedModule"] = "./Module",
                        ["app.mfe.route"] = "/plugin"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                }
            ]));

        var sut = new MfeSyncService(
            dockerClient,
            clientFactory,
            Substitute.For<ILogger<MfeSyncService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.SyncAfterDeployAsync("my-network", "my-stack", CancellationToken.None));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SyncAfterDeployAsync_DoesNotThrow_WhenDeployedStackHasNoPlugins_AndNoHostExists()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("MfeHostClient").Returns(new HttpClient(handler));

        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "svc-1",
                    State = "running",
                    Names = ["/my-service"],
                    Labels = new Dictionary<string, string>
                    {
                        ["com.docker.compose.project"] = "my-stack"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                }
            ]));

        var sut = new MfeSyncService(
            dockerClient,
            clientFactory,
            Substitute.For<ILogger<MfeSyncService>>());

        // Should not throw – no plugins, no host, just log and return.
        await sut.SyncAfterDeployAsync("my-network", "my-stack", CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SyncAfterDeployAsync_SendsBestEffort_WhenDeployedStackHasNoPlugins()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error")
            }));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("MfeHostClient").Returns(new HttpClient(handler));

        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "host-1",
                    State = "running",
                    Names = ["/mfe-host"],
                    Labels = new Dictionary<string, string>
                    {
                        [MfeLabels.HostEnabled] = "true",
                        [MfeLabels.HostApiKey] = "key"
                    },
                    Ports = [new Port { Type = "tcp", PrivatePort = 8080 }],
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                },
                new()
                {
                    ID = "svc-1",
                    State = "running",
                    Names = ["/my-service"],
                    Labels = new Dictionary<string, string>
                    {
                        ["com.docker.compose.project"] = "my-stack"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                }
            ]));

        var sut = new MfeSyncService(
            dockerClient,
            clientFactory,
            Substitute.For<ILogger<MfeSyncService>>());

        // Host returns 500 but stack has no plugins → best-effort, should not throw.
        // Retries all 5 attempts on 5xx before giving up silently.
        await sut.SyncAfterDeployAsync("my-network", "my-stack", CancellationToken.None);

        Assert.Equal(5, handler.Requests.Count);
    }

    [Fact]
    public async Task SyncAfterDeployAsync_RetriesAndThrows_WhenPluginStackAndHostFails()
    {
        var callCount = 0;
        var handler = new RecordingHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("starting up")
            });
        });

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("MfeHostClient").Returns(new HttpClient(handler));

        var dockerClient = Substitute.For<IDockerClient>();
        var containers = Substitute.For<IContainerOperations>();
        dockerClient.Containers.Returns(containers);

        containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<ContainerListResponse>>([
                new()
                {
                    ID = "host-1",
                    State = "running",
                    Names = ["/mfe-host"],
                    Labels = new Dictionary<string, string>
                    {
                        [MfeLabels.HostEnabled] = "true",
                        [MfeLabels.HostApiKey] = "key"
                    },
                    Ports = [new Port { Type = "tcp", PrivatePort = 8080 }],
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                },
                new()
                {
                    ID = "plugin-1",
                    State = "running",
                    Names = ["/my-plugin"],
                    Labels = new Dictionary<string, string>
                    {
                        ["com.docker.compose.project"] = "my-stack",
                        ["app.mfe.id"] = "todo",
                        ["app.mfe.entrypointPath"] = "remoteEntry.js",
                        ["app.mfe.displayName"] = "Todo",
                        ["app.mfe.exposedModule"] = "./Module",
                        ["app.mfe.route"] = "/todo"
                    },
                    NetworkSettings = new SummaryNetworkSettings
                    {
                        Networks = new Dictionary<string, EndpointSettings>
                        {
                            ["my-network"] = new()
                        }
                    }
                }
            ]));

        var sut = new MfeSyncService(
            dockerClient,
            clientFactory,
            Substitute.For<ILogger<MfeSyncService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.SyncAfterDeployAsync("my-network", "my-stack", CancellationToken.None));

        // Should have retried 5 times total.
        Assert.Equal(5, callCount);
    }
}
