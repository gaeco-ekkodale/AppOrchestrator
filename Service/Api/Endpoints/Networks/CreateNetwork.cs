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
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.Networks;

public class CreateNetworkRequest
{
    /// <summary>
    /// User-facing name used as both the display label and the Docker network name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Shared environment variables to inject into every stack on this network.
    /// </summary>
    public List<EnvironmentVariableInput> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Allowed version suffixes for update-channel filtering.
    /// Empty list means all versions are allowed.
    /// </summary>
    public List<string> AllowedVersionSuffixes { get; set; } = [];
}

/// <summary>
/// Input model for a single environment variable key-value pair.
/// </summary>
public class EnvironmentVariableInput
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class CreateNetworkRequestValidator : Validator<CreateNetworkRequest>
{
    public CreateNetworkRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256)
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9_.\-]*$")
            .WithMessage("Network name may only contain alphanumeric characters, underscores, dots and hyphens, and must start with a letter or digit.");

        RuleForEach(x => x.EnvironmentVariables).ChildRules(ev =>
        {
            ev.RuleFor(e => e.Name).NotEmpty().MaximumLength(100);
            ev.RuleFor(e => e.Value).NotNull().MaximumLength(500);
        });
    }
}

/// <summary>
/// Creates a Docker network and persists the entry in the database.
///
/// Workflow:
/// 1. Check for name conflict in DB
/// 2. Create the network in Docker
/// 3. Persist entry with the Docker-assigned network ID
/// </summary>
public class CreateNetwork(
    INetworkRepository networkRepository,
    IDockerNetworkService dockerNetworkService)
    : Endpoint<CreateNetworkRequest, NetworkDTO, NetworkMapper>
{
    public override void Configure()
    {
        Post("networks");
        Summary(s =>
        {
            s.Summary = "Create network.";
            s.Description = "Creates a Docker bridge network with the given name and saves it to the database. The name is immutable and serves as the stable identifier in both Docker and the database.";
            s.Response<NetworkDTO>(201, "Network created in Docker and persisted.");
            s.Response(400, "Validation failed.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(409, "A network with this name already exists.");
        });
    }

    public override async Task HandleAsync(CreateNetworkRequest req, CancellationToken ct)
    {
        var existing = await networkRepository.GetByNameAsync(req.Name, ct);
        if (existing is not null)
        {
            ThrowError("A network with this name already exists.", 409);
            return;
        }

        // Create the Docker network only when it does not already exist.
        // If it already exists externally (e.g. created via CLI or another compose stack)
        // we simply register it in the orchestrator without touching Docker.
        var alreadyExistsInDocker = await dockerNetworkService.ExistsAsync(req.Name, ct);
        if (!alreadyExistsInDocker)
            await dockerNetworkService.CreateNetworkAsync(req.Name, ct);

        var network = new Network
        {
            Name = req.Name,
            EnvironmentVariables = req.EnvironmentVariables.Select(ev => new EnvironmentVariable
            {
                Name = ev.Name,
                Value = ev.Value
            }).ToList(),
            AllowedVersionSuffixes = req.AllowedVersionSuffixes
                .Where(s => s is not null)
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(s => new AllowedVersionSuffix { Suffix = s })
                .ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await networkRepository.AddAsync(network, ct);
        await SendAsync(Map.FromEntity(network), 201, ct);
    }
}
