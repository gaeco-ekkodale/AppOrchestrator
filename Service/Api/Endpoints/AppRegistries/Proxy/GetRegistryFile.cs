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
/// Proxies GET /api/applications/{packageId}/versions/{version}/files/{fileName} to the specified App Registry.
/// Streams the file content so that binary assets (icons, compose files) are not buffered.
///
/// Package files are authored by whoever published the package, so everything except images is
/// served as a download rather than rendered: an HTML or SVG package file would otherwise execute
/// in the Orchestrator's origin with the viewer's session.
/// </summary>
public class GetRegistryFile(IRegistryProxyService proxyService)
    : EndpointWithoutRequest
{
    /// <summary>
    /// Content types served inline because the UI embeds them as images. Everything else is a
    /// download, so no package-authored markup is ever rendered in this origin.
    /// </summary>
    private static readonly string[] InlineContentTypes =
        ["image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"];

    public override void Configure()
    {
        Get("app-registries/{registryId}/applications/{packageId}/versions/{version}/files/{**fileName}");
        Summary(s =>
        {
            s.Summary = "Download a file from a specific version of a package.";
            s.Description = "Proxies the file download to the external App Registry using the stored API key. Supports docker-compose.yaml, .env.schema.yaml, icons, README.md, etc.";
            s.Response(200, "File content streamed.");
            s.Response(401, "Not authenticated.");
            s.Response(404, "Registry, application, version, or file not found.");
            s.Response(502, "Upstream registry error.");
        });
        DontAutoSendResponse();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var registryId = Route<Guid>("registryId");
        var packageId = Route<string>("packageId")!;
        var version = Route<string>("version")!;
        var fileName = Route<string>("fileName")!;

        var (stream, contentType) = await proxyService.GetFileAsync(registryId, packageId, version, fileName, ct);
        await using (stream)
        {
            var isInline = InlineContentTypes.Contains(
                contentType.Split(';')[0].Trim(), StringComparer.OrdinalIgnoreCase);

            HttpContext.Response.ContentType = isInline ? contentType : "application/octet-stream";
            HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";

            if (isInline)
            {
                // An SVG opened directly would otherwise run its own scripts in this origin.
                HttpContext.Response.Headers.ContentSecurityPolicy = "sandbox";
            }
            else
            {
                var safeName = Path.GetFileName(fileName);
                HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{safeName}\"";
            }

            await stream.CopyToAsync(HttpContext.Response.Body, ct);
        }
    }
}
