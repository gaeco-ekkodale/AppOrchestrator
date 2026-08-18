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
using AppOrchestrator.Api.Shared.Routing;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks;

/// <summary>
/// Starts all stopped containers of a stack, or runs docker compose up -d when no
/// containers exist yet.
/// </summary>
public class StartStack(
    IDockerProjectService dockerProjectService,
    ILogger<StartStack> logger)
    : Endpoint<StackRouteParams>
{
    public override void Configure()
    {
        Post("stacks/{projectName}/start");
        Description(x => x.Accepts<StackRouteParams>());
        Summary(s =>
        {
            s.Summary = "Start stack.";
            s.Description = "Starts all stopped containers for the compose project identified by projectName. When no containers exist yet, falls back to docker compose up -d using the persisted workspace definition.";
            s.Response(204, "Stack started successfully.");
            s.Response(400, "Route parameter projectName is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(500, "Docker returned an error while starting containers.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        logger.LogInformation("Starting stack {ProjectName}", projectName);

        await dockerProjectService.StartProjectAsync(projectName, ct);

        await SendNoContentAsync(ct);
    }
}
