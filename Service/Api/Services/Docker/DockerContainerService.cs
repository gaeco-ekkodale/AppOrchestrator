// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Docker;
using AppOrchestrator.Api.Shared.DTOs;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Globalization;

namespace AppOrchestrator.Api.Services.Docker;

/// <inheritdoc cref="IDockerContainerService"/>
public class DockerContainerService(
    ILogger<DockerContainerService> logger,
    IDockerClient dockerClient)
    : IDockerContainerService
{
    private const int DefaultTail = 400;
    private const int DefaultLimit = 400;
    private const int MaxTail = 5000;
    private const int MaxLimit = 5000;
    private const string LogTimestampDisplayFormat = "yyyy-MM-dd HH:mm:ss";

    // -----------------------------------------------------------------------
    // Container-level operations
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ContainerDTO>> ListContainersAsync(string projectName, CancellationToken ct = default)
    {
        var containers = await GetProjectContainersAsync(projectName, all: true, ct);
        return containers.Select(MapContainer).ToList();
    }

    /// <inheritdoc/>
    public async Task<ContainerDTO?> GetContainerAsync(string projectName, string containerId, CancellationToken ct = default)
    {
        var containers = await ListContainersAsync(projectName, ct);

        return containers.FirstOrDefault(c =>
            c.Id.Equals(containerId, StringComparison.OrdinalIgnoreCase) ||
            c.Id.StartsWith(containerId, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(containerId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public Task<ContainerLogsResponseDTO> GetContainerLogsAsync(
        string projectName,
        string containerId,
        string? since,
        int tail,
        int limit,
        CancellationToken ct = default)
        => GetContainerLogsInternalAsync(projectName, containerId, since, tail, limit, ct);

    /// <inheritdoc/>
    public Task StartContainerAsync(string projectName, string containerId, CancellationToken ct = default)
        => StartContainerInternalAsync(projectName, containerId, ct);

    /// <inheritdoc/>
    public Task StopContainerAsync(string projectName, string containerId, CancellationToken ct = default)
        => StopContainerInternalAsync(projectName, containerId, ct);

    /// <inheritdoc/>
    public async Task RestartContainerAsync(string projectName, string containerId, CancellationToken ct = default)
    {
        await StopContainerInternalAsync(projectName, containerId, ct);
        await StartContainerInternalAsync(projectName, containerId, ct);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task StartContainerInternalAsync(string projectName, string containerId, CancellationToken ct)
    {
        var container = await ResolveProjectContainerAsync(projectName, containerId, ct);
        await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), ct);
        logger.LogInformation("Started container {Container} in project {Project}", containerId, projectName);
    }

    private async Task StopContainerInternalAsync(string projectName, string containerId, CancellationToken ct)
    {
        var container = await ResolveProjectContainerAsync(projectName, containerId, ct);
        await dockerClient.Containers.StopContainerAsync(
            container.ID,
            new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
            ct);
        logger.LogInformation("Stopped container {Container} in project {Project}", containerId, projectName);
    }

    private async Task<ContainerLogsResponseDTO> GetContainerLogsInternalAsync(
        string projectName,
        string containerId,
        string? since,
        int tail,
        int limit,
        CancellationToken ct)
    {
        var container = await ResolveProjectContainerAsync(projectName, containerId, ct);

        var normalizedTailCount = tail <= 0 ? DefaultTail : Math.Clamp(tail, 1, MaxTail);
        var normalizedLimit = limit <= 0 ? DefaultLimit : Math.Clamp(limit, 1, MaxLimit);
        var sinceCursor = ParseSinceCursor(since);
        var sinceUnixSeconds = sinceCursor?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) ?? "0";

        using var stream = await dockerClient.Containers.GetContainerLogsAsync(
            container.ID,
            false,
            new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Timestamps = true,
                Tail = normalizedTailCount.ToString(CultureInfo.InvariantCulture),
                Since = sinceUnixSeconds
            },
            ct);

        var (stdout, stderr) = await stream.ReadOutputToEndAsync(ct);

        var lines = ParseLogLines(stdout, "stdout")
            .Concat(ParseLogLines(stderr, "stderr"))
            .Where(line => sinceCursor is null || ParseTimestamp(line.Timestamp) > sinceCursor)
            .OrderBy(line => line.Timestamp, StringComparer.Ordinal)
            .TakeLast(normalizedLimit)
            .ToList();

        var nextSince = ParseTimestamp(lines.LastOrDefault()?.Timestamp ?? string.Empty)?.ToString("O") ??
                (sinceCursor?.ToString("O") ?? DateTimeOffset.UtcNow.ToString("O"));

        return new ContainerLogsResponseDTO
        {
            ContainerId = containerId,
            NextSince = nextSince,
            Lines = lines
        };
    }

    private async Task<ContainerListResponse> ResolveProjectContainerAsync(
        string projectName,
        string containerId,
        CancellationToken ct)
    {
        static bool IdOrNameMatches(ContainerListResponse container, string requested)
        {
            if (container.ID.StartsWith(requested, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(container.ID, requested, StringComparison.OrdinalIgnoreCase))
                return true;

            var normalizedName = requested.TrimStart('/');
            return container.Names.Any(name =>
                string.Equals(name.TrimStart('/'), normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        var projectContainers = await GetProjectContainersAsync(projectName, all: true, ct);
        var match = projectContainers.FirstOrDefault(c => IdOrNameMatches(c, containerId));

        if (match is null)
        {
            // Fallback: search all containers and filter by compose project label.
            var allContainers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);

            match = allContainers.FirstOrDefault(c =>
            {
                if (!IdOrNameMatches(c, containerId))
                    return false;

                return c.Labels.TryGetValue(ComposeProjectLabel, out var project)
                    ? string.Equals(project, projectName, StringComparison.Ordinal)
                    : true;
            });
        }

        if (match is null)
            throw new KeyNotFoundException($"Container '{containerId}' was not found in stack '{projectName}'.");

        return match;
    }

    private Task<IList<ContainerListResponse>> GetProjectContainersAsync(
        string projectName,
        bool all,
        CancellationToken ct) =>
        dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = all,
                Filters = ProjectFilter(projectName)
            },
            ct);

    private const string ComposeProjectLabel = "com.docker.compose.project";

    private static Dictionary<string, IDictionary<string, bool>> ProjectFilter(string projectName) =>
        new()
        {
            ["label"] = new Dictionary<string, bool>
            {
                [$"{ComposeProjectLabel}={projectName}"] = true
            }
        };

    private static DateTimeOffset? ParseSinceCursor(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
            return null;

        return DateTimeOffset.TryParse(since, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? ParseTimestamp(string timestamp) =>
        DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;

    private static IEnumerable<ContainerLogLineDTO> ParseLogLines(string payload, string stream)
    {
        if (string.IsNullOrWhiteSpace(payload))
            yield break;

        using var reader = new StringReader(payload);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var split = line.IndexOf(' ');
            if (split <= 0)
            {
                yield return new ContainerLogLineDTO
                {
                    Timestamp = DateTimeOffset.UtcNow.ToString(LogTimestampDisplayFormat, CultureInfo.InvariantCulture),
                    Stream = stream,
                    Message = line
                };
                continue;
            }

            yield return new ContainerLogLineDTO
            {
                Timestamp = ParseTimestamp(line[..split].Trim())?.ToString(LogTimestampDisplayFormat, CultureInfo.InvariantCulture)
                            ?? DateTimeOffset.UtcNow.ToString(LogTimestampDisplayFormat, CultureInfo.InvariantCulture),
                Stream = stream,
                Message = line[(split + 1)..]
            };
        }
    }

    private static ContainerDTO MapContainer(ContainerListResponse c) => new()
    {
        Id = c.ID[..Math.Min(12, c.ID.Length)],
        Name = c.Names.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
        Service = c.Labels.TryGetValue("com.docker.compose.service", out var svc) ? svc : string.Empty,
        Image = c.Image,
        State = c.State,
        Status = c.Status,
        Ports = c.Ports.Select(FormatPort).Where(p => !string.IsNullOrEmpty(p)).ToList(),
        TraefikUrl = ExtractTraefikUrl(c.Labels)
    };

    /// <summary>
    /// Scans container labels for a Traefik router rule of the form
    /// <c>traefik.http.routers.&lt;name&gt;.rule=Host(`hostname`)</c> and builds
    /// the corresponding URL, respecting the optional <c>.tls=true</c> label.
    /// Returns <c>null</c> when no matching label is found.
    /// </summary>
    private static string? ExtractTraefikUrl(IDictionary<string, string> labels)
    {
        const string rulePrefix = "traefik.http.routers.";
        const string ruleSuffix = ".rule";
        const string entrypointsSuffix = ".entrypoints";

        foreach (var (key, value) in labels)
        {
            if (!key.StartsWith(rulePrefix, StringComparison.OrdinalIgnoreCase)
                || !key.EndsWith(ruleSuffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var match = System.Text.RegularExpressions.Regex.Match(value, @"Host\(`([^`]+)`\)");
            if (!match.Success)
                continue;

            var rawHost = match.Groups[1].Value;

            // If the extracted value already contains a scheme, use it as-is.
            if (rawHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawHost.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return rawHost;

            var routerName = key[rulePrefix.Length..^ruleSuffix.Length];
            var entrypointsKey = $"{rulePrefix}{routerName}{entrypointsSuffix}";
            // Accept both "websecure" (conventional) and "https" (common alternative) as TLS entrypoints.
            var scheme = labels.TryGetValue(entrypointsKey, out var ep)
                && (ep.Contains("websecure", StringComparison.OrdinalIgnoreCase)
                    || ep.Contains("https", StringComparison.OrdinalIgnoreCase))
                ? "https" : "http";

            return $"{scheme}://{rawHost}";
        }

        return null;
    }

    private static string FormatPort(Port port)
    {
        if (port.PublicPort > 0)
            return string.IsNullOrEmpty(port.IP)
                ? $"{port.PublicPort}->{port.PrivatePort}/{port.Type}"
                : $"{port.IP}:{port.PublicPort}->{port.PrivatePort}/{port.Type}";

        return $"{port.PrivatePort}/{port.Type}";
    }
}
