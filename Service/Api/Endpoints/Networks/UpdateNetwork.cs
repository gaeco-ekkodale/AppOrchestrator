// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Endpoints.Networks;
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Networks;

public class UpdateNetworkRequest
{
    /// <summary>
    /// Name of the network to update (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The full set of shared environment variables for the network (replace semantics).
    /// </summary>
    public List<EnvironmentVariableInput> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Allowed version suffixes for update-channel filtering (replace semantics).
    /// Empty list removes all restrictions.
    /// </summary>
    public List<string> AllowedVersionSuffixes { get; set; } = [];
}

public class UpdateNetworkRequestValidator : Validator<UpdateNetworkRequest>
{
    public UpdateNetworkRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleForEach(x => x.EnvironmentVariables).ChildRules(ev =>
        {
            ev.RuleFor(e => e.Name).NotEmpty().MaximumLength(100);
            ev.RuleFor(e => e.Value).NotNull().MaximumLength(500);
        });
    }
}

/// <summary>
/// Updates the shared environment variables of an existing network.
/// Uses replace semantics – the supplied list fully replaces the current variables.
/// </summary>
public class UpdateNetwork(INetworkRepository networkRepository)
    : Endpoint<UpdateNetworkRequest, NetworkDTO, NetworkMapper>
{
    public override void Configure()
    {
        Put("networks/{name}");
        Summary(s =>
        {
            s.Summary = "Update network.";
            s.Description = "Replaces the shared environment variables for the network identified by name. All stacks deployed on this network will receive the updated variables on their next compose up.";
            s.Response<NetworkDTO>(200, "Network updated.");
            s.Response(400, "Validation failed.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Network not found.");
        });
    }

    public override async Task HandleAsync(UpdateNetworkRequest req, CancellationToken ct)
    {
        var network = await networkRepository.GetByNameAsync(req.Name, ct);
        if (network is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        network.EnvironmentVariables = req.EnvironmentVariables.Select(ev => new EnvironmentVariable
        {
            Name = ev.Name,
            Value = ev.Value
        }).ToList();

        network.AllowedVersionSuffixes = req.AllowedVersionSuffixes
            .Where(s => s is not null)
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(s => new AllowedVersionSuffix { Suffix = s })
            .ToList();

        network.UpdatedAt = DateTime.UtcNow;

        await networkRepository.UpdateAsync(network, ct);
        await SendOkAsync(Map.FromEntity(network), ct);
    }
}
