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
/// Proxies GET /api/applications to the specified App Registry using the stored API key.
/// </summary>
public class GetRegistryApplications(IRegistryProxyService proxyService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("app-registries/{registryId}/applications");
        Summary(s =>
        {
            s.Summary = "List all applications from a registry.";
            s.Description = "Proxies the request to the external App Registry using the stored API key.";
            s.Response(200, "Applications returned as JSON.");
            s.Response(401, "Not authenticated.");
            s.Response(404, "Registry not found.");
            s.Response(502, "Upstream registry error.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var registryId = Route<Guid>("registryId");
        var json = await proxyService.GetApplicationsAsync(registryId, ct);

        HttpContext.Response.ContentType = "application/json";
        await HttpContext.Response.Body.WriteAsync(json, ct);
    }
}
