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
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Domain.Models;

namespace AppOrchestrator.Api.Services.Stacks;

/// <inheritdoc cref="IAppRegistryClient"/>
public class AppRegistryClient(
    IHttpClientFactory httpClientFactory,
    IRegistrySecretProtector secretProtector,
    ILogger<AppRegistryClient> logger)
    : IAppRegistryClient
{
    /// <inheritdoc/>
    public async Task<Stream> FetchComposeFileAsync(
        AppRegistry registry,
        string packageId,
        string version,
        CancellationToken ct = default)
    {
        var client = CreateAuthenticatedClient(registry);
        var effectiveBaseUrl = RegistryNetworkHelper.NormalizeBaseUrl(registry.BaseUrl, logger);

        var url = $"{effectiveBaseUrl.TrimEnd('/')}/api/applications/{Uri.EscapeDataString(packageId)}/versions/{Uri.EscapeDataString(version)}/files/docker-compose.yaml";

        logger.LogInformation("Fetching compose file from registry: {Url}", url);

        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadPackageZipAsync(
        AppRegistry registry,
        string packageId,
        string version,
        CancellationToken ct = default)
    {
        var client = CreateAuthenticatedClient(registry);
        var effectiveBaseUrl = RegistryNetworkHelper.NormalizeBaseUrl(registry.BaseUrl, logger);

        var url = $"{effectiveBaseUrl.TrimEnd('/')}/api/applications/{Uri.EscapeDataString(packageId)}/versions/{Uri.EscapeDataString(version)}/download";

        logger.LogInformation("Downloading package ZIP from registry: {Url}", url);

        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(ct);
    }

    private HttpClient CreateAuthenticatedClient(AppRegistry registry)
    {
        var client = httpClientFactory.CreateClient("RegistryClient");

        var apiKey = secretProtector.Unprotect(registry.ApiKeyEncrypted);
        if (!string.IsNullOrEmpty(apiKey))
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        else
            logger.LogWarning("Registry '{RegistryId}' has no API key configured.", registry.Id);

        return client;
    }
}
