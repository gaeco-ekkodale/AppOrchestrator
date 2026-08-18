// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.Routing;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.Stacks;

/// <summary>
/// Response payload containing editable stack compose and env content.
/// </summary>
public class StackComposeResponse
{
    /// <summary>
    /// User-facing stack name.
    /// </summary>
    public string StackName { get; set; } = string.Empty;

    /// <summary>
    /// Full docker-compose.yml content from the stack workspace.
    /// </summary>
    public string ComposeContent { get; set; } = string.Empty;

    /// <summary>
    /// Effective environment variables represented as key-value pairs.
    /// </summary>
    public Dictionary<string, string> EnvConfig { get; set; } = [];
}

/// <summary>
/// Returns the editable compose and env content for a custom stack.
/// Only supported for stacks that are not linked to an application registry.
/// </summary>
public class GetCompose(
    IStackRepository stackRepository,
    IFileService fileService)
    : Endpoint<StackRouteParams, StackComposeResponse>
{
    public override void Configure()
    {
        Get("stacks/{projectName}/compose");
        Summary(s =>
        {
            s.Summary = "Get compose and env for stack.";
            s.Description = "Loads the editable docker-compose.yml and .env content from the workspace of a custom stack identified by docker project name.";
            s.Response<StackComposeResponse>(200, "Compose and env content for the stack.");
            s.Response(400, "Route parameter is invalid, or the stack is registry-managed and does not support direct compose editing.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No managed stack exists for the provided project name.");
        });
    }

    public override async Task HandleAsync(StackRouteParams req, CancellationToken ct)
    {
        var projectName = req.ProjectName;

        var stack = await stackRepository.GetAsync(projectName, ct);
        if (stack is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (stack is RegistryStack)
        {
            ThrowError("This stack is linked to a registry. Use stack version updates instead of direct compose editing.", 400);
            return;
        }

        var workspacePath = fileService.GetInternalWorkspacePath(stack.DockerProjectName);
        await SendOkAsync(new StackComposeResponse
        {
            StackName = stack.StackName,
            ComposeContent = await fileService.ReadComposeFileAsync(workspacePath, ct),
            EnvConfig = await fileService.ReadEnvFileAsync(workspacePath, ct)
        }, ct);
    }
}