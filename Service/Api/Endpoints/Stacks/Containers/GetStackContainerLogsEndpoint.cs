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
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Stacks.Containers;

public class GetStackContainerLogsRequest : StackContainerRouteParams
{
    public string? Since { get; set; }

    public int? Tail { get; set; }

    public int? Limit { get; set; }
}

public class GetStackContainerLogsRequestValidator : Validator<GetStackContainerLogsRequest>
{
    public GetStackContainerLogsRequestValidator()
    {
        RuleFor(x => x.Since).MaximumLength(100).When(x => x.Since is not null);
        RuleFor(x => x.Tail).GreaterThanOrEqualTo(0).When(x => x.Tail.HasValue);
        RuleFor(x => x.Limit).GreaterThanOrEqualTo(0).When(x => x.Limit.HasValue);
    }
}


/// <summary>
/// Returns cursor-based logs for one container.
/// </summary>
public class GetStackContainerLogsEndpoint(
    IDockerContainerService dockerContainerService)
    : Endpoint<GetStackContainerLogsRequest, ContainerLogsResponseDTO>
{
    public override void Configure()
    {
        Get("stacks/{projectName}/containers/{containerId}/logs");
        Summary(s =>
        {
            s.Summary = "Get container logs.";
            s.Description = "Returns log lines for a container with cursor-based incremental polling. Use 'since' from the previous response as the next request cursor.";
            s.Response<ContainerLogsResponseDTO>(200, "Container logs response with next cursor.");
            s.Response(400, "Route parameters are missing or query values are invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Container was not found in the specified stack.");
        });
    }

    public override async Task HandleAsync(GetStackContainerLogsRequest req, CancellationToken ct)
    {
        var projectName = req.ProjectName;
        var containerId = req.ContainerId;

        var since = req.Since;
        var tail = req.Tail ?? 0;
        var limit = req.Limit ?? 0;

        var result = await dockerContainerService.GetContainerLogsAsync(projectName, containerId, since, tail, limit, ct);
        await SendOkAsync(result, ct);
    }
}

