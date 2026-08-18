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
/// Stops all running containers of a stack without removing them.
/// </summary>
public class StopStack(
    IDockerProjectService dockerProjectService,
    ILogger<StopStack> logger)
    : Endpoint<StackRouteParams>
{
    public override void Configure()
    {
        Post("stacks/{projectName}/stop");
        Description(x => x.Accepts<StackRouteParams>());
        Summary(s =>
        {
            s.Summary = "Stop stack.";
            s.Description = "Stops all running containers for the compose project identified by projectName without removing them.";
            s.Response(204, "Stack stopped successfully.");
            s.Response(400, "Route parameter projectName is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(500, "Docker returned an error while stopping containers.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        logger.LogInformation("Stopping stack {ProjectName}", projectName);

        await dockerProjectService.StopProjectAsync(projectName, ct);

        await SendNoContentAsync(ct);
    }
}
