// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Api.Shared.Routing;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Stacks;

public class UpdateStackComposeRequest : StackRouteParams
{
    public string ComposeContent { get; set; } = string.Empty;

    public Dictionary<string, string> EnvConfig { get; set; } = [];
}

public class UpdateStackComposeRequestValidator : Validator<UpdateStackComposeRequest>
{
    public UpdateStackComposeRequestValidator()
    {
        RuleFor(x => x.ComposeContent).NotEmpty().WithMessage("Compose content must not be empty.");
        RuleFor(x => x.EnvConfig).Must(env => env.All(kv => !string.IsNullOrWhiteSpace(kv.Key)))
            .WithMessage("EnvConfig keys must be non-empty strings.")
            .Must(env => env.All(kv => kv.Key.Length <= 100))
            .WithMessage("EnvConfig keys must be at most 100 characters long.")
            .Must(env => env.All(kv => kv.Value.Length <= 500))
            .WithMessage("EnvConfig values must be at most 500 characters long.");
    }
}

public class UpdateStackComposeEndpoint(
    IStackDeploymentService stackDeploymentService)
    : Endpoint<UpdateStackComposeRequest, StackComposeResponse>
{
    public override void Configure()
    {
        Put("stacks/{projectName}/compose");
        Summary(s =>
        {
            s.Summary = "Update compose and env for stack.";
            s.Description = "Writes a new docker-compose.yml and .env for a custom stack identified by docker project name, then executes docker compose up to apply the changes.";
            s.Response<StackComposeResponse>(200, "Compose and env were updated and the latest persisted content is returned.");
            s.Response(400, "Invalid payload or stack is registry-managed.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No managed stack exists for the provided project name.");
            s.Response(500, "The compose update could not be applied. Please verify compose and environment values.");
        });
    }

    public override async Task HandleAsync(UpdateStackComposeRequest req, CancellationToken ct)
    {
        try
        {
            var response = await stackDeploymentService.UpdateComposeAsync(
                new UpdateStackComposeCommand(req.ProjectName, req.ComposeContent, req.EnvConfig),
                ct);

            await SendOkAsync(new StackComposeResponse
            {
                StackName = response.StackName,
                ComposeContent = response.ComposeContent,
                EnvConfig = response.EnvConfig
            }, ct);
        }
        catch (KeyNotFoundException)
        {
            await SendNotFoundAsync(ct);
        }
        catch (ArgumentException ex)
        {
            ThrowError(ex.Message, 400);
        }
        catch (InvalidOperationException)
        {
            ThrowError("Die Compose-Aenderung konnte nicht angewendet werden. Bitte pruefe die bereitgestellten Compose- und Umgebungswerte.", 500);
        }
    }
}

