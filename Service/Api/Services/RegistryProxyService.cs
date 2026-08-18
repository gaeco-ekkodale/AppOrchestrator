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
using AppOrchestrator.Api.Services.RegistryApi;
using AppOrchestrator.Api.Services.Stacks;
using AppOrchestrator.Domain.Repositories;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AppOrchestrator.Api.Services;

/// <inheritdoc cref="IRegistryProxyService"/>
public class RegistryProxyService(
    IAppRegistryRepository registryRepository,
    IRegistrySecretProtector secretProtector,
    IHttpClientFactory httpClientFactory,
    ILogger<RegistryProxyService> logger)
    : IRegistryProxyService
{
    private static readonly string[] UrlFields = ["iconUrl", "readmeUrl", "manifestUrl", "downloadUrl"];

    public async Task<byte[]> GetApplicationsAsync(Guid registryId, CancellationToken ct = default)
    {
        var (client, _) = await BuildApiClientAsync(registryId, ct);
        var apps = await client.GetAllAppsEndpointAsync(ct);
        return Serialize(apps, registryId);
    }

    public async Task<byte[]> GetApplicationAsync(Guid registryId, string packageId, CancellationToken ct = default)
    {
        var (client, _) = await BuildApiClientAsync(registryId, ct);
        var app = await client.GetAppEndpointAsync(packageId, ct);
        return Serialize(app, registryId);
    }

    public async Task<byte[]> GetVersionsAsync(Guid registryId, string packageId, CancellationToken ct = default)
    {
        var (client, _) = await BuildApiClientAsync(registryId, ct);
        var versions = await client.GetAppVersionsEndpointAsync(packageId, ct);
        return Serialize(versions, registryId);
    }

    public async Task<byte[]> GetVersionAsync(Guid registryId, string packageId, string version, CancellationToken ct = default)
    {
        var (client, _) = await BuildApiClientAsync(registryId, ct);
        var ver = await client.GetAppVersionEndpointAsync(packageId, version, ct);
        return Serialize(ver, registryId);
    }

    public async Task<(Stream Content, string ContentType)> GetFileAsync(
        Guid registryId, string packageId, string version, string fileName, CancellationToken ct = default)
    {
        // DownloadAppFileEndpointAsync in the generated client returns void and discards the response stream.
        // We reuse the same HttpClient (with API key already set) and mirror the generated URL pattern.
        var (apiClient, httpClient) = await BuildApiClientAsync(registryId, ct);
        var url = $"{apiClient.BaseUrl}api/applications/{Uri.EscapeDataString(packageId)}/versions/{Uri.EscapeDataString(version)}/files/{Uri.EscapeDataString(fileName)}";

        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        return (await response.Content.ReadAsStreamAsync(ct), contentType);
    }

    private async Task<(AppRegistryApiClient ApiClient, HttpClient HttpClient)> BuildApiClientAsync(
        Guid registryId, CancellationToken ct)
    {
        var registry = await registryRepository.GetByIdAsync(registryId, ct)
            ?? throw new KeyNotFoundException($"Registry '{registryId}' was not found.");

        var httpClient = httpClientFactory.CreateClient("RegistryClient");

        var apiKey = secretProtector.Unprotect(registry.ApiKeyEncrypted);
        if (!string.IsNullOrEmpty(apiKey))
            httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        else
            logger.LogWarning("Registry '{RegistryId}' has no API key configured.", registryId);

        var baseUrl = RegistryNetworkHelper.NormalizeBaseUrl(registry.BaseUrl, logger);
        var apiClient = new AppRegistryApiClient(httpClient) { BaseUrl = baseUrl };
        return (apiClient, httpClient);
    }

    private byte[] Serialize(object? value, Guid registryId)
    {
        var json = JsonConvert.SerializeObject(value);
        return Encoding.UTF8.GetBytes(RewriteUrls(json, registryId));
    }

    /// <summary>
    /// Rewrites absolute registry URLs inside iconUrl/readmeUrl JSON fields
    /// to Orchestrator proxy file endpoints so the browser never calls the registry directly.
    /// </summary>
    private string RewriteUrls(string json, Guid registryId)
    {
        try
        {
            var token = JToken.Parse(json);
            RewriteToken(token, registryId);
            return token.ToString(Formatting.None);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "URL rewrite skipped: JSON parse failed.");
            return json;
        }
    }

    private void RewriteToken(JToken token, Guid registryId)
    {
        switch (token.Type)
        {
            case JTokenType.Array:
                foreach (var child in token.Children())
                    RewriteToken(child, registryId);
                break;

            case JTokenType.Object:
                var obj = (JObject)token;
                foreach (var field in UrlFields)
                {
                    if (obj[field] is JValue { Type: JTokenType.String } val
                        && val.Value<string>() is { } raw
                        && TryExtractFileInfo(raw, out var pkgId, out var ver, out var fn))
                    {
                        obj[field] = $"/api/app-registries/{registryId}/applications/{pkgId}/versions/{ver}/files/{fn}";
                    }
                }
                foreach (var child in obj.Children<JProperty>())
                    RewriteToken(child.Value, registryId);
                break;
        }
    }

    /// <summary>
    /// Extracts packageId, version, and fileName from an absolute registry file URL.
    /// Expected pattern: .../api/applications/{packageId}/versions/{version}/files/{fileName}
    /// </summary>
    private static bool TryExtractFileInfo(string url, out string packageId, out string version, out string fileName)
    {
        packageId = version = fileName = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // Expected: api, applications, {pkgId}, versions, {ver}, files, {file}
        var filesIdx = Array.IndexOf(segments, "files");
        if (filesIdx < 4 || filesIdx >= segments.Length - 1)
            return false;

        fileName = segments[filesIdx + 1];
        version = segments[filesIdx - 1];
        packageId = segments[filesIdx - 3];
        return true;
    }
}
