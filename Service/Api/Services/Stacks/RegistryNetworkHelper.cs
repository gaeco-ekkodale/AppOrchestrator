// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Net;

namespace AppOrchestrator.Api.Services.Stacks;

/// <summary>
/// Shared URL normalisation helpers for reaching external registry services.
/// When the orchestrator runs inside Docker, localhost in registry base URLs is
/// rewritten to host.docker.internal so containers can reach services on the host.
/// </summary>
internal static class RegistryNetworkHelper
{
    internal static string NormalizeBaseUrl(string baseUrl, ILogger logger)
    {
        if (!IsRunningInContainer())
            return baseUrl;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return baseUrl;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return baseUrl;

        var isLocalHost = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(uri.Host, out var ip)
            && IPAddress.IsLoopback(ip);

        if (!isLocalHost)
            return baseUrl;

        var builder = new UriBuilder(uri) { Host = "host.docker.internal" };
        var rewritten = builder.Uri.ToString().TrimEnd('/');
        logger.LogInformation(
            "Rewriting app registry base URL for container runtime: {OriginalBaseUrl} -> {RewrittenBaseUrl}",
            baseUrl, rewritten);
        return rewritten;
    }

    internal static bool IsRunningInContainer()
    {
        var value = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }
}
