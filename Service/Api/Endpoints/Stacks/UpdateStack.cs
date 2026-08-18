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
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Api.Shared.Routing;
using AppOrchestrator.Api.Shared.Utils;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Stacks;

public class UpdateStackRequest : StackRouteParams
{
    public string? StackName { get; set; }

    public string? Version { get; set; }

    public Dictionary<string, string>? EnvConfig { get; set; }

    /// <summary>
    /// Reassigns the stack to a different network by name.
    /// Set to <c>""</c> (empty string) to detach the stack from its current network
    /// without assigning a new one.
    /// </summary>
    public string? NetworkName { get; set; }
}

public class UpdateStackRequestValidator : StackRouteValidator<UpdateStackRequest>
{
    public UpdateStackRequestValidator()
    {
        RuleFor(x => x.StackName).MaximumLength(200).When(x => x.StackName is not null);
        RuleFor(x => x.Version).MaximumLength(100).When(x => x.Version is not null);
        RuleFor(x => x.EnvConfig)
            .Must(env => env!.All(kv => !string.IsNullOrWhiteSpace(kv.Key)))
            .WithMessage("EnvConfig keys must be non-empty strings.")
            .Must(env => env!.All(kv => kv.Key.Length <= 100))
            .WithMessage("EnvConfig keys must be at most 100 characters long.")
            .Must(env => env!.All(kv => kv.Value.Length <= 500))
            .WithMessage("EnvConfig values must be at most 500 characters long.")
            .When(x => x.EnvConfig is not null);
        RuleFor(x => x.NetworkName).MaximumLength(100).When(x => x.NetworkName is not null);
    }
}

public class UpdateStackEndpoint(
    IStackDeploymentService stackDeploymentService,
    INetworkRepository networkRepository,
    IStackRepository stackRepository)
    : Endpoint<UpdateStackRequest, StackDTO, StackMapper>
{
    public override void Configure()
    {
        Put("stacks/{projectName}");
        Summary(s =>
        {
            s.Summary = "Update stack.";
            s.Description = "Supports partial updates for a stack identified by docker project name. Rename requires a stopped stack. Version updates fetch a new compose file from the linked registry and apply it with backup handling.";
            s.Response<StackDTO>(200, "Stack was updated and returned with current runtime status.");
            s.Response(400, "No supported fields were provided or a custom stack attempted a version update.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Stack or linked registry was not found.");
            s.Response(409, "Rename conflict occurred or stack must be stopped before renaming.");
            s.Response(502, "Linked upstream services are currently unavailable.");
        });
    }

    public override async Task HandleAsync(UpdateStackRequest req, CancellationToken ct)
    {
        // Guard: if a new version is being deployed, check it against the target network's allowed suffixes.
        if (req.Version is not null)
        {
            var stack = await stackRepository.GetAsync(req.ProjectName, ct);
            if (stack is null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            // Use the new network if the assignment is being changed; fall back to the current one.
            var effectiveNetworkName = req.NetworkName ?? stack.NetworkName;
            if (!string.IsNullOrWhiteSpace(effectiveNetworkName))
            {
                var network = await networkRepository.GetByNameAsync(effectiveNetworkName, ct);
                if (network is not null && network.AllowedVersionSuffixes.Count > 0 &&
                    !VersionCheck.IsVersionAllowed(req.Version, network.AllowedVersionSuffixes))
                {
                    ThrowError(
                        $"Version '{req.Version}' ist für das Environment '{effectiveNetworkName}' nicht zulässig. " +
                        $"Erlaubte Kanäle: {string.Join(", ", network.AllowedVersionSuffixes.Select(s => s.Suffix == "" ? "(stabil)" : $"-{s.Suffix}"))}.",
                        400);
                }
            }
        }

        try
        {
            var stack = await stackDeploymentService.UpdateAsync(
                new UpdateStackCommand(req.ProjectName, req.StackName, req.Version, req.EnvConfig, req.NetworkName),
                ct);
            await SendOkAsync(Map.FromEntity(stack), ct);
        }
        catch (ArgumentException ex)
        {
            ThrowError(ex.Message, 400);
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

