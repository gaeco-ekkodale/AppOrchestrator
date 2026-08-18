// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Mfe;
using AppOrchestrator.Api.Services._Interfaces.Stacks;
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Mappers;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Stacks;

public class CreateCustomStackRequest
{
    public string StackName { get; set; } = string.Empty;
    public string ComposeContent { get; set; } = string.Empty;
    public Dictionary<string, string> EnvConfig { get; set; } = [];

    /// <summary>
    /// Optional name of an orchestrator-managed network to connect this stack to.
    /// The name will be injected as <c>NETWORK_NAME</c> in the .env file.
    /// </summary>
    public string? NetworkName { get; set; }
}

public class CreateCustomStackRequestValidator : Validator<CreateCustomStackRequest>
{
    public CreateCustomStackRequestValidator()
    {
        RuleFor(x => x.StackName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ComposeContent).NotEmpty();
        RuleFor(x => x.EnvConfig)
            .Must(env => env.All(kv => !string.IsNullOrWhiteSpace(kv.Key)))
            .WithMessage("EnvConfig keys must be non-empty strings.")
            .Must(env => env.All(kv => kv.Key.Length <= 100))
            .WithMessage("EnvConfig keys must be at most 100 characters long.")
            .Must(env => env.All(kv => kv.Value.Length <= 500))
            .WithMessage("EnvConfig values must be at most 500 characters long.");
    }
}

/// <summary>
/// Deploys a stack from a raw docker-compose.yml pasted by the user (no AppRegistry required).
///
/// Workflow:
/// 1. Validate name uniqueness
/// 2. Write docker-compose.yml from the supplied content + .env from EnvConfig
/// 3. Run docker compose up -d
/// 4. Persist Stack entity (RegistryId = null)
/// 5. Return StackDTO (Running or Failed)
/// </summary>
public class CreateCustomStack(
    IStackDeploymentService stackDeploymentService,
    IMfeSyncService mfeSyncService)
    : Endpoint<CreateCustomStackRequest, StackDTO, StackMapper>
{
    public override void Configure()
    {
        Post("stacks/custom");
        Summary(s =>
        {
            s.Summary = "Deploy stack from custom compose.";
            s.Description = "Deploys a stack from raw docker-compose content supplied by the client. The API stores stack metadata with no linked application registry.";
            s.Response<StackDTO>(201, "Stack was deployed and persisted successfully.");
            s.Response(400, "Request payload validation failed.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(409, "Another stack already uses the same derived project name.");
            s.Response(502, "The plugin host in the target network is currently unavailable.");
        });
    }

    public override async Task HandleAsync(CreateCustomStackRequest req, CancellationToken ct)
    {
        try
        {
            var stack = await stackDeploymentService.CreateCustomAsync(
                new CreateCustomStackCommand(req.StackName, req.ComposeContent, req.EnvConfig, req.NetworkName ?? ""),
                ct);

            if (!string.IsNullOrWhiteSpace(stack.NetworkName))
            {
                try
                {
                    await mfeSyncService.SyncAfterDeployAsync(stack.NetworkName, stack.DockerProjectName, ct);
                }
                catch (HttpRequestException ex)
                {
                    await stackDeploymentService.DeleteAsync(stack.DockerProjectName, ct);
                    var hostMessage = string.IsNullOrWhiteSpace(ex.Message)
                        ? "Der Plugin-Host hat die Anfrage abgelehnt."
                        : ex.Message;
                    ThrowError(hostMessage, 502);
                }
                catch
                {
                    await stackDeploymentService.DeleteAsync(stack.DockerProjectName, ct);
                    ThrowError("Stack wurde bereitgestellt, aber der Plugin-Host im Netzwerk konnte nicht synchronisiert werden. Der Rollback wurde ausgefuehrt. Bitte pruefen, ob ein Host-Container mit orchestrator.host=true und orchestrator.apiKey vorhanden ist.", 502);
                }
            }

            await SendAsync(Map.FromEntity(stack), 201, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 409);
        }
        catch (HttpRequestException)
        {
            ThrowError("Ein benoetigter Upstream-Service ist momentan nicht erreichbar. Bitte spaeter erneut versuchen.", 502);
        }
    }
}


