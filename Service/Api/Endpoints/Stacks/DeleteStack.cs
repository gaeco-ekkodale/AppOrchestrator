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
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks;

/// <summary>
/// Stops and removes all Docker resources of a stack, deletes the workspace directory,
/// and removes the DB record.
/// </summary>
public class DeleteStack(
    IDockerProjectService dockerProjectService,
    IFileService fileService,
    IStackRepository stackRepository,
    ILogger<DeleteStack> logger)
    : Endpoint<StackRouteParams>
{
    public override void Configure()
    {
        Delete("stacks/{projectName}");
        Description(x => x.Accepts<StackRouteParams>());
        Summary(s =>
        {
            s.Summary = "Delete stack.";
            s.Description = "Stops and removes all Docker containers and networks for the compose project, deletes the workspace directory, and removes the database record if one exists.";
            s.Response(204, "Stack resources removed and database record deleted.");
            s.Response(400, "Route parameter projectName is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(500, "Docker returned an error while removing containers.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        logger.LogInformation("Deleting stack {ProjectName}", projectName);

        // 1. Stop and remove all Docker containers and networks for the project.
        await dockerProjectService.RemoveProjectAsync(projectName, ct);

        // 2. Delete the workspace directory from disk.
        var workspacePath = fileService.GetInternalWorkspacePath(projectName);
        if (fileService.DirectoryExists(workspacePath))
            fileService.DeleteDirectory(workspacePath);

        // 3. Remove the database record (no-op if none exists).
        await stackRepository.DeleteAsync(projectName, ct);

        await SendNoContentAsync(ct);
    }
}
