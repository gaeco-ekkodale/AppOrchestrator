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
using AppOrchestrator.Api.Shared.Routing;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks.Containers;

public class GetStackContainerEndpoint(
    IDockerContainerService dockerContainerService)
    : Endpoint<StackContainerRouteParams, ContainerDTO>
{
    public override void Configure()
    {
        Get("stacks/{projectName}/containers/{containerId}");
        Summary(s =>
        {
            s.Summary = "Get container by id.";
            s.Description = "Returns one container from a stack by matching short/full id or container name.";
            s.Response<ContainerDTO>(200, "Container found.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Container was not found in the specified stack.");
        });
    }

    public override async Task HandleAsync(StackContainerRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;
        var containerId = req.ContainerId;

        var match = await dockerContainerService.GetContainerAsync(projectName, containerId, ct);

        if (match is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(match, ct);
    }
}