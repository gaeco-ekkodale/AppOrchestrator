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
using AppOrchestrator.Api.Shared.Utils;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Stacks;

public class CreateStackRequest
{
    public string StackName { get; set; } = string.Empty;
    public Guid RegistryId { get; set; }
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> EnvConfig { get; set; } = [];
    public string NetworkName { get; set; } = string.Empty;
}

public class CreateStackRequestValidator : Validator<CreateStackRequest>
{
    public CreateStackRequestValidator()
    {
        RuleFor(x => x.StackName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PackageId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EnvConfig)
            .Must(env => env.All(kv => !string.IsNullOrWhiteSpace(kv.Key)))
            .WithMessage("EnvConfig keys must be non-empty strings.")
            .Must(env => env.All(kv => kv.Key.Length <= 100))
            .WithMessage("EnvConfig keys must be at most 100 characters long.")
            .Must(env => env.All(kv => kv.Value.Length <= 500))
            .WithMessage("EnvConfig values must be at most 500 characters long.");
        RuleFor(x => x.NetworkName).MaximumLength(100);
    }
}

/// <summary>
/// Deploys an application from an AppRegistry.
///
/// Workflow:
/// 1. Resolve registry base URL from DB
/// 2. Fetch docker-compose.yml from registry
/// 3. Write workspace (compose + .env)
/// 4. Run docker compose up -d
/// 5. Persist Stack entity
/// 6. Return StackDTO (Running or Failed)
/// </summary>
public class CreateStack(
    IStackDeploymentService stackDeploymentService,
    IMfeSyncService mfeSyncService,
    INetworkRepository networkRepository)
    : Endpoint<CreateStackRequest, StackDTO, StackMapper>
{
    public override void Configure()
    {
        Post("stacks");
        Summary(s =>
        {
            s.Summary = "Deploy stack from registry package.";
            s.Description = "Creates a new stack from a package version in an application registry. The endpoint fetches compose content, writes workspace files, executes docker compose up, and persists stack metadata.";
            s.Response<StackDTO>(201, "Stack was deployed and persisted successfully.");
            s.Response(400, "Request payload validation failed.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Referenced registry does not exist.");
            s.Response(409, "Another stack already uses the same derived project name.");
            s.Response(502, "Required upstream services (registry or plugin host) are currently unavailable.");
        });
    }

    public override async Task HandleAsync(CreateStackRequest req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.NetworkName))
        {
            var network = await networkRepository.GetByNameAsync(req.NetworkName, ct);
            if (network is not null && network.AllowedVersionSuffixes.Count > 0 &&
                !VersionCheck.IsVersionAllowed(req.Version, network.AllowedVersionSuffixes))
            {
                ThrowError(
                    $"Version '{req.Version}' ist für das Environment '{req.NetworkName}' nicht zulässig. " +
                    $"Erlaubte Kanäle: {string.Join(", ", network.AllowedVersionSuffixes.Select(s => s.Suffix == "" ? "(stabil)" : $"-{s.Suffix}"))}.",
                    400);
            }
        }

        try
        {
            var stack = await stackDeploymentService.CreateFromRegistryAsync(
                new CreateStackFromRegistryCommand(
                    req.StackName,
                    req.RegistryId,
                    req.PackageId,
                    req.Version,
                    req.EnvConfig,
                    req.NetworkName),
                ct);

            // Sync the plugin host. If the deployed stack contains MFE plugins the
            // sync is strict (with retry) and rolls back on failure. Stacks without
            // plugins only trigger a best-effort sync that never blocks the deployment.
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
        catch (KeyNotFoundException)
        {
            await SendNotFoundAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 409);
        }
        catch (HttpRequestException)
        {
            ThrowError("Das Quell-Registry-System ist momentan nicht erreichbar. Bitte spaeter erneut versuchen.", 502);
        }
    }
}
