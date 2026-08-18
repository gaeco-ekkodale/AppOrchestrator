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
/// Restarts all containers of a stack.
/// </summary>
public class RestartStack(
    IDockerProjectService dockerProjectService,
    ILogger<RestartStack> logger)
    : Endpoint<StackRouteParams>
{
    public override void Configure()
    {
        Post("stacks/{projectName}/restart");
        Description(x => x.Accepts<StackRouteParams>());
        Summary(s =>
        {
            s.Summary = "Restart stack.";
            s.Description = "Restarts all containers for the Docker Compose project identified by projectName.";
            s.Response(204, "Stack restarted successfully.");
            s.Response(400, "Route parameter projectName is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(500, "Docker returned an error while restarting containers.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        logger.LogInformation("Restarting stack {ProjectName}", projectName);

        await dockerProjectService.RestartProjectAsync(projectName, ct);

        await SendNoContentAsync(ct);
    }
}
