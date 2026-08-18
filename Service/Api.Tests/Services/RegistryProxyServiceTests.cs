// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services;
using AppOrchestrator.Api.Services._Interfaces;
using AppOrchestrator.Api.Tests.Services.Http;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using System.Net;
using System.Text;

namespace AppOrchestrator.Api.Tests.Services;

public class RegistryProxyServiceTests
{
    private static readonly Guid RegistryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ─── Factory helpers ────────────────────────────────────────────────────

    private static (RegistryProxyService Sut, RecordingHttpMessageHandler Handler) Build(
        string responseJson,
        string? apiKey = null)
    {
        var registry = new AppRegistry
        {
            Id = RegistryId,
            Name = "Test",
            BaseUrl = "https://registry.local/",
            ApiKeyEncrypted = apiKey != null ? "encrypted" : null
        };

        var repo = Substitute.For<IAppRegistryRepository>();
        repo.GetByIdAsync(RegistryId, Arg.Any<CancellationToken>()).Returns(registry);

        var protector = Substitute.For<IRegistrySecretProtector>();
        protector.Unprotect(Arg.Any<string?>()).Returns(apiKey);

        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            }));

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

        var sut = new RegistryProxyService(repo, protector, factory, Substitute.For<ILogger<RegistryProxyService>>());
        return (sut, handler);
    }

    private static RegistryProxyService BuildWithMissingRegistry()
    {
        var repo = Substitute.For<IAppRegistryRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AppRegistry?)null);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        return new RegistryProxyService(
            repo,
            Substitute.For<IRegistrySecretProtector>(),
            factory,
            Substitute.For<ILogger<RegistryProxyService>>());
    }

    // ─── GetApplicationsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetApplicationsAsync_ThrowsKeyNotFoundException_WhenRegistryDoesNotExist()
    {
        var sut = BuildWithMissingRegistry();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetApplicationsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetApplicationsAsync_ReturnsAllApplicationsFromRegistry()
    {
        var json = JsonConvert.SerializeObject(new[]
        {
            MinimalApp("pkg-1", "App One"),
            MinimalApp("pkg-2", "App Two")
        });

        var (sut, _) = Build(json);
        var resultBytes = await sut.GetApplicationsAsync(RegistryId);

        var apps = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(Encoding.UTF8.GetString(resultBytes))!;
        Assert.Equal(2, apps.Count);
        Assert.Contains(apps, a => a["packageId"]?.ToString() == "pkg-1");
        Assert.Contains(apps, a => a["packageId"]?.ToString() == "pkg-2");
    }

    [Fact]
    public async Task GetApplicationsAsync_RewritesIconUrl_ToOrchestratorProxyPath()
    {
        var registryIconUrl = "https://registry.local/api/applications/my-pkg/versions/1.0/files/icon.png";
        var json = JsonConvert.SerializeObject(new[] { MinimalApp("my-pkg", "My App", iconUrl: registryIconUrl) });

        var (sut, _) = Build(json);
        var resultBytes = await sut.GetApplicationsAsync(RegistryId);
        var resultJson = Encoding.UTF8.GetString(resultBytes);

        // iconUrl must point to the Orchestrator proxy — the browser must never reach the registry
        Assert.Contains($"/api/app-registries/{RegistryId}/applications/my-pkg/versions/1.0/files/icon.png", resultJson);
        Assert.DoesNotContain("https://registry.local/api/applications", resultJson);
    }

    [Fact]
    public async Task GetApplicationsAsync_AddsApiKeyHeader_WhenKeyIsConfigured()
    {
        var json = JsonConvert.SerializeObject(new[] { MinimalApp("pkg", "App") });
        var (sut, handler) = Build(json, apiKey: "secret-api-key");

        await sut.GetApplicationsAsync(RegistryId);

        Assert.Single(handler.Requests);
        Assert.Equal("secret-api-key", handler.Requests[0].Headers.GetValues("X-API-Key").Single());
    }

    [Fact]
    public async Task GetApplicationsAsync_OmitsApiKeyHeader_WhenNoKeyConfigured()
    {
        var json = JsonConvert.SerializeObject(new[] { MinimalApp("pkg", "App") });
        var (sut, handler) = Build(json, apiKey: null);

        await sut.GetApplicationsAsync(RegistryId);

        Assert.Single(handler.Requests);
        Assert.False(handler.Requests[0].Headers.Contains("X-API-Key"));
    }

    // ─── GetFileAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFileAsync_ThrowsKeyNotFoundException_WhenRegistryDoesNotExist()
    {
        var sut = BuildWithMissingRegistry();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetFileAsync(Guid.NewGuid(), "pkg", "1.0", "icon.png"));
    }

    [Fact]
    public async Task GetFileAsync_ReturnsFileContentAndContentType()
    {
        var fileBytes = "compose: {}"u8.ToArray();

        var repo = Substitute.For<IAppRegistryRepository>();
        var registry = new AppRegistry { Id = RegistryId, Name = "Test", BaseUrl = "https://registry.local/" };
        repo.GetByIdAsync(RegistryId, Arg.Any<CancellationToken>()).Returns(registry);

        var protector = Substitute.For<IRegistrySecretProtector>();
        protector.Unprotect(Arg.Any<string?>()).Returns((string?)null);

        var handler = new RecordingHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(fileBytes);
            response.Content.Headers.ContentType = new("application/yaml");
            return Task.FromResult(response);
        });

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

        var sut = new RegistryProxyService(repo, protector, factory, Substitute.For<ILogger<RegistryProxyService>>());
        var (stream, contentType) = await sut.GetFileAsync(RegistryId, "pkg", "1.0.0", "docker-compose.yaml");

        Assert.Equal("application/yaml", contentType);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(fileBytes, ms.ToArray());
    }

    [Fact]
    public async Task GetFileAsync_ThrowsHttpRequestException_WhenRegistryReturnsError()
    {
        var repo = Substitute.For<IAppRegistryRepository>();
        var registry = new AppRegistry { Id = RegistryId, Name = "Test", BaseUrl = "https://registry.local/" };
        repo.GetByIdAsync(RegistryId, Arg.Any<CancellationToken>()).Returns(registry);

        var protector = Substitute.For<IRegistrySecretProtector>();
        protector.Unprotect(Arg.Any<string?>()).Returns((string?)null);

        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)));

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

        var sut = new RegistryProxyService(repo, protector, factory, Substitute.For<ILogger<RegistryProxyService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.GetFileAsync(RegistryId, "pkg", "1.0.0", "docker-compose.yaml"));
    }

    // ─── Shared test data ────────────────────────────────────────────────────

    private static object MinimalApp(
        string packageId,
        string name,
        string iconUrl = "https://registry.local/api/applications/pkg/versions/1.0/files/icon.png") => new
    {
        packageId,
        name,
        description = (string?)null,
        ownerId = Guid.Empty,
        ownerUsername = "testuser",
        defaultVersion = "1.0",
        iconUrl,
        createdAt = DateTimeOffset.UtcNow
    };
}
