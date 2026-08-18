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

namespace AppOrchestrator.Api.Endpoints.ContainerRegistries;

/// <summary>
/// Request payload for creating a container registry connection.
/// </summary>
public class CreateContainerRegistryRequest
{
    /// <summary>
    /// Display name of the registry connection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Registry host name or URL used by Docker login, for example ghcr.io.
    /// </summary>
    public string ServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Username used for docker authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password or access token used for docker authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Validates create-container-registry payload rules.
/// </summary>
public class CreateContainerRegistryValidator : Validator<CreateContainerRegistryRequest>
{
    public CreateContainerRegistryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServerAddress).NotEmpty().MaximumLength(500).Matches(@"^https?://|^[^/\s]+$").WithMessage("Server address must be a valid hostname or URL.");
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

/// <summary>
/// Creates a container registry entry and verifies credentials with Docker.
/// </summary>
public class CreateContainerRegistry(
    IContainerRegistryRepository containerRegistryRepository,
    IDockerRegistryService dockerRegistryService)
    : Endpoint<CreateContainerRegistryRequest, ContainerRegistryDTO, ContainerRegistryMapper>
{
    public override void Configure()
    {
        Post("container-registries");
        Summary(s =>
        {
            s.Summary = "Create container registry.";
            s.Description = "Creates a persistent registry entry and validates credentials by executing docker login before saving. Credentials are only used for validation and are not returned in responses.";
            s.Response<ContainerRegistryDTO>(201, "Registry was created after successful docker authentication.");
            s.Response(400, "Validation failed or docker login was rejected by the registry.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(409, "Another registry entry already uses the same server address.");
        });
    }

    public override async Task HandleAsync(CreateContainerRegistryRequest req, CancellationToken ct)
    {
        var existing = await containerRegistryRepository.GetByServerAddressAsync(req.ServerAddress, ct);
        if (existing is not null)
        {
            ThrowError("A container registry with this server address already exists.", 409);
            return;
        }

        var (success, output) = await dockerRegistryService.LoginAsync(req.ServerAddress, req.Username, req.Password, ct);
        if (!success)
        {
            AddError(r => r.Password, $"docker login failed: {output}");
            ThrowIfAnyErrors(400);
            return;
        }

        var registry = new ContainerRegistry
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            ServerAddress = req.ServerAddress,
            CreatedAt = DateTime.UtcNow
        };

        await containerRegistryRepository.AddAsync(registry, ct);
        await SendAsync(Map.FromEntity(registry), 201, ct);
    }
}

