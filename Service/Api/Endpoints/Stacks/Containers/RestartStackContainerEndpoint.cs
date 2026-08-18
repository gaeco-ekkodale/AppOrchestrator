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

public class RestartStackContainerEndpoint(
    IDockerContainerService dockerContainerService)
    : Endpoint<StackContainerRouteParams>
{
    public override void Configure()
    {
        Post("stacks/{projectName}/containers/{containerId}/restart");
        Description(x => x.Accepts<StackContainerRouteParams>());
        Summary(s =>
        {
            s.Summary = "Restart container.";
            s.Description = "Stops then starts a single container that belongs to the given stack.";
            s.Response(204, "Container restarted successfully.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(400, "Route parameters are missing.");
        });
    }

    public override async Task HandleAsync(StackContainerRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;
        var containerId = req.ContainerId;

        await dockerContainerService.RestartContainerAsync(projectName, containerId, ct);
        await SendNoContentAsync(ct);
    }
}