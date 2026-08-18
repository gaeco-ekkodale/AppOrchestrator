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

namespace AppOrchestrator.Api.Endpoints.Stacks.Containers;

/// <summary>
/// Stops one container of a stack.
/// </summary>
public class StopStackContainerEndpoint(
    IDockerContainerService dockerContainerService)
    : Endpoint<StackContainerRouteParams>
{
    public override void Configure()
    {
        Post("stacks/{projectName}/containers/{containerId}/stop");
        Description(x => x.Accepts<StackContainerRouteParams>());
        Summary(s =>
        {
            s.Summary = "Stop container.";
            s.Description = "Stops a single Docker container that belongs to the compose project identified by projectName.";
            s.Response(204, "Container stopped successfully.");
            s.Response(400, "Route parameters are missing.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Container was not found in the specified stack.");
        });
    }

    public override async Task HandleAsync(StackContainerRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;
        var containerId = req.ContainerId;

        await dockerContainerService.StopContainerAsync(projectName, containerId, ct);
        await SendNoContentAsync(ct);
    }
}
