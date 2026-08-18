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

/// <summary>
/// Returns all Docker containers that belong to a stack, including stopped ones.
/// </summary>
public class ListStackContainersEndpoint(
    IDockerContainerService dockerContainerService)
    : Endpoint<StackRouteParams, List<ContainerDTO>>
{
    public override void Configure()
    {
        Get("stacks/{projectName}/containers");
        Summary(s =>
        {
            s.Summary = "List containers for a stack.";
            s.Description = "Queries the Docker Engine API for all containers (including stopped) that belong to the compose project identified by route id (docker project name).";
            s.Response<List<ContainerDTO>>(200, "List of containers with state, status and port information.");
            s.Response(400, "Route parameter id is missing.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        var containers = await dockerContainerService.ListContainersAsync(projectName, ct);
        await SendOkAsync(containers.ToList(), ct);
    }
}
