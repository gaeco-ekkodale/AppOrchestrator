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
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Api.Shared.Routing;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks;

/// <summary>
/// Returns detailed information for one stack, including live runtime status,
/// container list, and parsed env configuration from the workspace.
/// Falls back to Docker-discovered external stacks when no database entry exists.
/// </summary>
public class GetStack(
    IStackRepository stackRepository,
    IDockerProjectService dockerProjectService,
    IFileService fileService)
    : Endpoint<StackRouteParams, StackDetailsDTO, StackDetailsMapper>
{
    public override void Configure()
    {
        Get("stacks/{projectName}");
        Summary(s =>
        {
            s.Summary = "Get stack by project name.";
            s.Description = "Loads one stack by project name, resolves current runtime status and container list from Docker, and returns parsed env configuration from the workspace .env file. Falls back to Docker-discovered external stacks when no managed DB entry exists.";
            s.Response<StackDetailsDTO>(200, "Detailed stack payload including env key-value pairs and containers.");
            s.Response(400, "Route parameter projectName is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No stack exists for the provided project name.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        var stack = await stackRepository.GetAsync(projectName, ct);

        if (stack is null)
        {
            // Fall back to Docker discovery for stacks not in the database.
            var dockerProjects = await dockerProjectService.ListComposeProjectNamesAsync(ct);
            if (!dockerProjects.Contains(projectName))
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var externalWorkspacePath = fileService.GetInternalWorkspacePath(projectName);
            var externalDto = new StackDetailsDTO
            {
                StackName = projectName,
                DockerProjectName = projectName,
                Status = await dockerProjectService.GetProjectStatusAsync(projectName, ct),
                Source = StackSource.External
            };

            await SendOkAsync(externalDto, ct);
            return;
        }

        // Persisted stack: enrich with live status, containers and env config.
        var dto = Map.FromEntity(stack);
        var workspacePath = fileService.GetInternalWorkspacePath(stack.DockerProjectName);

        dto.Status = await dockerProjectService.GetProjectStatusAsync(stack.DockerProjectName, ct);
        dto.EnvConfig = await fileService.ReadEnvFileAsync(workspacePath, ct);

        await SendOkAsync(dto, ct);
    }
}
