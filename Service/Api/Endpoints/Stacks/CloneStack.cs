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
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Stacks;

public class CloneStackRequest : StackRouteParams
{
    /// <summary>Display name for the clone. Omit to keep the source name (requires a different network).</summary>
    public string? NewStackName { get; set; }

    /// <summary>Network for the clone. Omit to keep the source network (requires a different name).</summary>
    public string? NetworkName { get; set; }
}

public class CloneStackRequestValidator : StackRouteValidator<CloneStackRequest>
{
    public CloneStackRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.NewStackName) || x.NetworkName is not null)
            .WithMessage("Provide a new stack name, a network, or both.");
    }
}

/// <summary>
/// Creates a new stack record from an existing one by copying its complete workspace,
/// including package files and volume data.
/// The clone starts in Stopped state - use POST /stacks/{projectName}/start to run it.
/// To change env vars before starting, use PUT /stacks/{projectName}.
/// </summary>
public class CloneStack(
    IStackDeploymentService stackDeploymentService)
    : Endpoint<CloneStackRequest, StackDTO, StackMapper>
{
    public override void Configure()
    {
        Post("stacks/{projectName}/clone");
        Summary(s =>
        {
            s.Summary = "Clone stack metadata and workspace.";
            s.Description = "Copies the full workspace of an existing stack - compose, env, package files and volume data - into a new workspace and creates a new stack record. Since the project name is derived from stack name and network, at least one of them must differ from the source. No docker command is executed during cloning.";
            s.Response<StackDTO>(201, "Clone was created successfully.");
            s.Response(400, "Neither name nor network differs from the source.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Source managed stack does not exist for the provided project name.");
            s.Response(409, "Target stack name already exists on the target network.");
        });
    }

    public override async Task HandleAsync(CloneStackRequest req, CancellationToken ct)
    {
        try
        {
            var stack = await stackDeploymentService.CloneAsync(
                new CloneStackCommand(req.ProjectName, req.NewStackName, req.NetworkName), ct);
            await SendAsync(Map.FromEntity(stack), 201, ct);
        }
        catch (KeyNotFoundException)
        {
            await SendNotFoundAsync(ct);
        }
        catch (ArgumentException ex)
        {
            ThrowError(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 409);
        }
    }

}



