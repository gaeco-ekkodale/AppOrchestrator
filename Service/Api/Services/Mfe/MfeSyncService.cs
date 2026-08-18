// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Mfe;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppOrchestrator.Api.Services.Mfe;

/// <inheritdoc cref="IMfeSyncService"/>
public class MfeSyncService(
    IDockerClient dockerClient,
    IHttpClientFactory httpClientFactory,
    ILogger<MfeSyncService> logger)
    : IMfeSyncService
{
    private const string SyncPath = "/api/plugins";
    private const string LabelHostEnabledValue = "true";
    private const string ComposeProjectLabel = "com.docker.compose.project";
    private const int MaxRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task SyncNetworkAsync(
        string networkName,
        CancellationToken ct = default)
    {
        try
        {
            await SyncCoreAsync(networkName, deployedProjectName: null, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MFE sync for network [{Network}] failed.", networkName);
        }
    }

    /// <inheritdoc/>
    public async Task SyncAfterDeployAsync(
        string networkName,
        string dockerProjectName,
        CancellationToken ct = default)
    {
        try
        {
            await SyncCoreAsync(networkName, dockerProjectName, ct);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MFE sync for network [{Network}] failed.", networkName);
        }
    }

    // -----------------------------------------------------------------------
    // Core
    // -----------------------------------------------------------------------

    private async Task SyncCoreAsync(
        string networkName,
        string? deployedProjectName,
        CancellationToken ct)
    {
        var allContainers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true }, ct);

        var hostResolution = ResolveHost(allContainers, networkName);

        var strict = deployedProjectName is not null
            && allContainers
                .Where(c => BelongsToProject(c, deployedProjectName))
                .Any(IsPluginContainer);

        if (!hostResolution.Success)
        {
            if (strict)
                throw new HttpRequestException(
                    $"Plugin host could not be resolved for network '{networkName}': {hostResolution.ErrorMessage}");

            logger.LogWarning("MFE sync skipped for network [{Network}] - {Reason}",
                networkName, hostResolution.ErrorMessage);
            return;
        }

        var plugins = allContainers
            .Where(c => c.NetworkSettings?.Networks?.ContainsKey(networkName) == true)
            .Where(IsPluginContainer)
            .Select(c => BuildPlugin(c, networkName))
            .ToList();

        logger.LogInformation(
            "MFE sync for network [{Network}]: {Count} MFE container(s) found.",
            networkName, plugins.Count);

        await PutSnapshotAsync(
            networkName,
            hostResolution.Target!.HostUrl,
            hostResolution.Target.ApiKey,
            new MfeSnapshot(plugins),
            strict,
            ct);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static bool IsPluginContainer(ContainerListResponse container) =>
        container.Labels.Keys.Any(MfeLabels.IsPluginLabel);

    private static bool BelongsToProject(ContainerListResponse container, string projectName) =>
        container.Labels.TryGetValue(ComposeProjectLabel, out var value)
        && string.Equals(value, projectName, StringComparison.OrdinalIgnoreCase);

    private static MfePayload BuildPlugin(ContainerListResponse container, string networkName)
    {
        var containerName = container.Names?.FirstOrDefault()?.TrimStart('/') ?? container.ID;

        return new MfePayload
        {
            Id = GetLabel(container.Labels, MfeLabels.Id) ?? containerName,
            DisplayName = GetLabel(container.Labels, MfeLabels.DisplayName) ?? containerName,
            Description = GetLabel(container.Labels, MfeLabels.Description),
            ContainerBaseUrl = BuildInternalUrl(container, networkName),
            IconPath = GetLabel(container.Labels, MfeLabels.IconPath),
            EntrypointPath = GetLabel(container.Labels, MfeLabels.EntrypointPath) ?? string.Empty,
            ExposedModule = GetLabel(container.Labels, MfeLabels.ExposedModule) ?? string.Empty,
            Route = GetLabel(container.Labels, MfeLabels.Route) ?? string.Empty,
            State = string.Equals(container.State, "running", StringComparison.OrdinalIgnoreCase)
                ? MfePluginState.Running
                : MfePluginState.Stopped
        };
    }

    private static string? GetLabel(IDictionary<string, string> labels, string key)
    {
        return labels.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static HostResolution ResolveHost(IList<ContainerListResponse> allContainers, string networkName)
    {
        var hosts = allContainers
            .Where(c => c.NetworkSettings?.Networks?.ContainsKey(networkName) == true)
            .Where(c =>
                c.Labels.TryGetValue(MfeLabels.HostEnabled, out var enabled)
                && string.Equals(enabled, LabelHostEnabledValue, StringComparison.OrdinalIgnoreCase)
                && c.Labels.TryGetValue(MfeLabels.HostApiKey, out var apiKey)
                && !string.IsNullOrWhiteSpace(apiKey))
            .ToList();

        if (hosts.Count == 0)
            return HostResolution.Fail($"no host container with labels '{MfeLabels.HostEnabled}=true' and '{MfeLabels.HostApiKey}' was found.");

        if (hosts.Count > 1)
            return HostResolution.Fail("multiple host containers were found. Expected exactly one host per network.");

        var hostContainer = hosts[0];

        var hostUrl = BuildInternalUrl(hostContainer, networkName);
        var apiKey = hostContainer.Labels[MfeLabels.HostApiKey];

        return HostResolution.Ok(new HostTarget(hostUrl, apiKey));
    }

    private static string BuildInternalUrl(ContainerListResponse container, string networkName)
    {
        // Docker DNS on user-defined networks resolves containers by their container name
        // (e.g. "myproject-myservice-1"), which is unique even when multiple Compose stacks
        // share the same network and happen to use the same service name.
        var containerName = container.Names?.FirstOrDefault()?.TrimStart('/');

        var firstTcpPort = container.Ports?.FirstOrDefault(p =>
            string.Equals(p.Type, "tcp", StringComparison.OrdinalIgnoreCase) && p.PrivatePort > 0);

        return firstTcpPort is not null
            ? $"http://{containerName}:{firstTcpPort.PrivatePort}"
            : $"http://{containerName}";
    }

    private async Task PutSnapshotAsync(
        string networkName,
        string hostUrl,
        string apiKey,
        MfeSnapshot payload,
        bool strict,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("MfeHostClient");
        var targetUrl = hostUrl.TrimEnd('/') + SyncPath;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, targetUrl)
                {
                    Content = JsonContent.Create(payload, options: JsonOpts)
                };
                if (!string.IsNullOrEmpty(apiKey))
                    request.Headers.Add("X-API-Key", apiKey);

                var response = await client.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "MFE sync for network [{Network}] succeeded ({Count} plugin(s)).",
                        networkName, payload.Plugins.Count);
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                var message = BuildHostErrorMessage(networkName, targetUrl, (int)response.StatusCode, body);
                lastError = new HttpRequestException(message, null, response.StatusCode);

                if (attempt < MaxRetries && response.StatusCode >= HttpStatusCode.InternalServerError)
                {
                    logger.LogWarning(
                        "MFE sync attempt {Attempt}/{Max} for [{Network}] returned {StatusCode}, retrying in {Delay}s…",
                        attempt, MaxRetries, networkName, (int)response.StatusCode, RetryDelay.TotalSeconds);
                    await Task.Delay(RetryDelay, ct);
                    continue;
                }

                break;
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
                if (attempt < MaxRetries)
                {
                    logger.LogWarning(
                        "MFE sync attempt {Attempt}/{Max} for [{Network}] failed, retrying in {Delay}s…",
                        attempt, MaxRetries, networkName, RetryDelay.TotalSeconds);
                    await Task.Delay(RetryDelay, ct);
                    continue;
                }

                break;
            }
        }

        if (strict)
            throw lastError!;

        logger.LogWarning(lastError,
            "MFE sync for network [{Network}] failed - host at {Url} may be unreachable.",
            networkName, targetUrl);
    }

    private static string BuildHostErrorMessage(string networkName, string targetUrl, int statusCode, string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return $"MFE sync for network [{networkName}] returned {statusCode} from {targetUrl}.";

        var raw = body.Trim();

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? raw;

            if (root.ValueKind != JsonValueKind.Object)
                return raw;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var validationMessages = errors.EnumerateObject()
                    .SelectMany(property =>
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            return property.Value.EnumerateArray()
                                .Where(v => v.ValueKind == JsonValueKind.String)
                                .Select(v => $"{property.Name}: {v.GetString()}");
                        }

                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            return [$"{property.Name}: {property.Value.GetString()}"];
                        }

                        return Array.Empty<string>();
                    })
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                if (validationMessages.Count > 0)
                    return string.Join(" | ", validationMessages);
            }

            if (TryGetStringProperty(root, "error", out var error))
                return error;
            if (TryGetStringProperty(root, "detail", out var detail))
                return detail;
            if (TryGetStringProperty(root, "message", out var message))
                return message;
            if (TryGetStringProperty(root, "title", out var title))
                return title;
        }
        catch
        {
            // Keep raw host response when payload is not JSON.
        }

        return raw;
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return false;

        var parsed = property.GetString();
        if (string.IsNullOrWhiteSpace(parsed))
            return false;

        value = parsed;
        return true;
    }

    private sealed record HostTarget(string HostUrl, string ApiKey);

    private sealed record HostResolution(bool Success, HostTarget? Target, string? ErrorMessage)
    {
        public static HostResolution Ok(HostTarget target) => new(true, target, null);

        public static HostResolution Fail(string errorMessage) => new(false, null, errorMessage);
    }
}