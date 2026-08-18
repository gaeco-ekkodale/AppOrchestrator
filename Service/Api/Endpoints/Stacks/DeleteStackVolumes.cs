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
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.Routing;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks;

public class DeleteStackVolumes(
    IDockerProjectService dockerProjectService,
    IFileService fileService,
    ILogger<DeleteStackVolumes> logger)
    : Endpoint<StackRouteParams>
{
    public override void Configure()
    {
        Delete("stacks/{projectName}/volumes");
        Description(x => x.Accepts<StackRouteParams>());
        Summary(s =>
        {
            s.Summary = "Delete stack volumes.";
            s.Description = "Removes all Docker volumes associated with the given compose project.";
            s.Response(204, "Volumes removed successfully.");
            s.Response(400, "Route parameter projectName is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(500, "An error occurred while removing the volumes.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        logger.LogInformation("Deleting volumes for stack {ProjectName}", projectName);

        await dockerProjectService.StopProjectAsync(projectName, ct);
        await fileService.DeleteVolumes(projectName);

        await SendNoContentAsync(ct);
    }
}
