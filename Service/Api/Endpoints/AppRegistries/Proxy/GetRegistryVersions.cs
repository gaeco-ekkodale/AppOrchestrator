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
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.AppRegistries.Proxy;

/// <summary>
/// Proxies GET /api/applications/{packageId}/versions to the specified App Registry.
/// </summary>
public class GetRegistryVersions(IRegistryProxyService proxyService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("app-registries/{registryId}/applications/{packageId}/versions");
        Summary(s =>
        {
            s.Summary = "List all versions of a package from a registry.";
            s.Description = "Proxies the request to the external App Registry using the stored API key.";
            s.Response(200, "Versions returned as JSON.");
            s.Response(401, "Not authenticated.");
            s.Response(404, "Registry or application not found.");
            s.Response(502, "Upstream registry error.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var registryId = Route<Guid>("registryId");
        var packageId = Route<string>("packageId")!;

        var json = await proxyService.GetVersionsAsync(registryId, packageId, ct);

        HttpContext.Response.ContentType = "application/json";
        await HttpContext.Response.Body.WriteAsync(json, ct);
    }
}
