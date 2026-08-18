// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces;
using AppOrchestrator.Api.Services.Stacks;
using AppOrchestrator.Api.Tests.Services.Http;
using AppOrchestrator.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;

namespace AppOrchestrator.Api.Tests.Services.Stacks;

public class AppRegistryClientTests
{
    private static IRegistrySecretProtector SecretProtector(string? key = "api-key-123")
    {
        var protector = Substitute.For<IRegistrySecretProtector>();
        protector.Unprotect(Arg.Any<string?>()).Returns(key);
        return protector;
    }

    [Fact]
    public async Task FetchComposeFileAsync_AddsApiKey_AndBuildsEncodedUrl()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("services:{}")
            }));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

        var registry = new AppRegistry
        {
            BaseUrl = "https://registry.local/",
            Name = "Test",
            ApiKeyEncrypted = "encrypted"
        };

        var sut = new AppRegistryClient(clientFactory, SecretProtector("api-key-123"), Substitute.For<ILogger<AppRegistryClient>>());

        await using var stream = await sut.FetchComposeFileAsync(
            registry,
            "my app/id",
            "1.0.0-beta+1",
            CancellationToken.None);

        Assert.Single(handler.Requests);
        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, request.Method);
        // OriginalString preserves the exact percent-encoded URL that Uri.EscapeDataString produced,
        // without Uri.ToString()'s normalization that decodes %20 back to a space.
        Assert.Equal(
            "https://registry.local/api/applications/my%20app%2Fid/versions/1.0.0-beta%2B1/files/docker-compose.yaml",
            request.RequestUri!.OriginalString);
        Assert.Equal("api-key-123", request.Headers.GetValues("X-API-Key").Single());
    }

    [Fact]
    public async Task FetchComposeFileAsync_OmitsApiKeyHeader_WhenNoKeyConfigured()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("services:{}")
            }));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

        var registry = new AppRegistry { BaseUrl = "https://registry.local", Name = "Test" };

        var sut = new AppRegistryClient(clientFactory, SecretProtector(null), Substitute.For<ILogger<AppRegistryClient>>());

        await using var _ = await sut.FetchComposeFileAsync(registry, "pkg", "1.0.0", CancellationToken.None);

        Assert.Single(handler.Requests);
        Assert.False(handler.Requests[0].Headers.Contains("X-API-Key"));
    }

    [Fact]
    public async Task FetchComposeFileAsync_ThrowsHttpRequestException_WhenResponseIsNotSuccessful()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

        var registry = new AppRegistry { BaseUrl = "https://registry.local", Name = "Test" };

        var sut = new AppRegistryClient(clientFactory, SecretProtector(null), Substitute.For<ILogger<AppRegistryClient>>());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.FetchComposeFileAsync(registry, "pkg", "1.0.0", CancellationToken.None));
    }

    [Fact]
    public async Task FetchComposeFileAsync_RewritesLocalhostToHostDockerInternal_WhenRunningInContainer()
    {
        var previous = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

        try
        {
            var handler = new RecordingHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("services:{}")
                }));

            var clientFactory = Substitute.For<IHttpClientFactory>();
            clientFactory.CreateClient("RegistryClient").Returns(new HttpClient(handler));

            var registry = new AppRegistry { BaseUrl = "http://localhost:8080", Name = "Test" };

            var sut = new AppRegistryClient(
                clientFactory,
                SecretProtector(null),
                Substitute.For<ILogger<AppRegistryClient>>());

            await using var _ = await sut.FetchComposeFileAsync(
                registry,
                "pkg",
                "1.0.0",
                CancellationToken.None);

            Assert.Single(handler.Requests);
            Assert.Equal(
                "http://host.docker.internal:8080/api/applications/pkg/versions/1.0.0/files/docker-compose.yaml",
                handler.Requests[0].RequestUri!.OriginalString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", previous);
        }
    }
}
