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
using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks;

/// <summary>
/// Returns all stack overviews, combining persisted database entries with stacks
/// discovered live from the Docker daemon that have no database record yet.
/// </summary>
public class GetAllStacks(
    IStackRepository stackRepository,
    IDockerProjectService dockerProjectService)
    : EndpointWithoutRequest<IEnumerable<StackDTO>, StackMapper>
{
    public override void Configure()
    {
        Get("stacks");
        Summary(s =>
        {
            s.Summary = "List stacks.";
            s.Description = "Returns persisted stacks enriched with live Docker status. Additionally includes Docker-discovered stacks that have no database record, reported as External source.";
            s.Response<IEnumerable<StackDTO>>(200, "Stack list including persisted metadata and live status.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var stacks = await stackRepository.ListAsync(ct);

        // Resolve live status for all persisted stacks in parallel.
        var statusTasks = stacks.Select(s => dockerProjectService.GetProjectStatusAsync(s.DockerProjectName, ct));
        var statuses = await Task.WhenAll(statusTasks);

        var result = stacks.Select((s, i) =>
        {
            var dto = Map.FromEntity(s);
            dto.Status = statuses[i];
            return dto;
        }).ToList();

        // Discover Docker-only stacks not yet in the database.
        var dockerProjectNames = await dockerProjectService.ListComposeProjectNamesAsync(ct);
        var dbProjectNames = stacks.Select(s => s.DockerProjectName).ToHashSet(StringComparer.Ordinal);

        foreach (var projectName in dockerProjectNames.Where(p => !dbProjectNames.Contains(p)))
        {
            var status = await dockerProjectService.GetProjectStatusAsync(projectName, ct);
            result.Add(new StackDTO
            {
                StackName = projectName,
                DockerProjectName = projectName,
                Status = status,
                Source = StackSource.External
            });
        }

        await SendOkAsync(result.OrderBy(x => x.StackName, StringComparer.OrdinalIgnoreCase), ct);
    }
}
