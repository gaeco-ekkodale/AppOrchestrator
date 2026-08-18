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
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.ContainerRegistries;

/// <summary>
/// Request payload for updating an existing container registry connection.
/// </summary>
public class UpdateContainerRegistryRequest
{
    /// <summary>
    /// Optional new display name for the registry entry.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional new server address. When changed, logout is attempted for the old address
    /// and login is executed against the new address.
    /// </summary>
    public string? ServerAddress { get; set; }

    /// <summary>
    /// Username used for validating and storing the updated login state.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password or access token used for validating and storing the updated login state.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Validates update-container-registry payload rules.
/// </summary>
public class UpdateContainerRegistryValidator : Validator<UpdateContainerRegistryRequest>
{
    public UpdateContainerRegistryValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.ServerAddress).MaximumLength(500).When(x => x.ServerAddress is not null).Matches(@"^https?://|^[^/\s]+$").WithMessage("Server address must be a valid hostname or URL.");
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

/// <summary>
/// Updates metadata and connectivity details of a container registry.
/// </summary>
public class UpdateContainerRegistry(
    IContainerRegistryRepository containerRegistryRepository,
    IDockerRegistryService dockerRegistryService)
    : Endpoint<UpdateContainerRegistryRequest, ContainerRegistryDTO, ContainerRegistryMapper>
{
    public override void Configure()
    {
        Put("container-registries/{id}");
        Summary(s =>
        {
            s.Summary = "Update container registry.";
            s.Description = "Performs a partial update on a registry entry and revalidates access using docker logout on the old address followed by docker login on the target address.";
            s.Response<ContainerRegistryDTO>(200, "Registry was updated and returned in its final persisted state.");
            s.Response(400, "Validation failed or docker login was rejected by the registry.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No registry exists for the provided id.");
            s.Response(409, "Another registry entry already uses the requested server address.");
        });
    }

    public override async Task HandleAsync(UpdateContainerRegistryRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var registry = await containerRegistryRepository.GetByIdAsync(id, ct);
        if (registry is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var oldAddress = registry.ServerAddress;
        var newAddress = req.ServerAddress ?? oldAddress;

        if (!string.Equals(newAddress, oldAddress, StringComparison.Ordinal))
        {
            var existing = await containerRegistryRepository.GetByServerAddressAsync(newAddress, ct);
            if (existing is not null && existing.Id != registry.Id)
            {
                ThrowError("A container registry with this server address already exists.", 409);
                return;
            }
        }

        var (success, output) = await dockerRegistryService.LoginAsync(newAddress, req.Username, req.Password, ct);
        if (!success)
        {
            AddError(r => r.Password, $"docker login failed: {output}");
            ThrowIfAnyErrors(400);
            return;
        }

        if (req.Name is not null)
            registry.Name = req.Name;

        registry.ServerAddress = newAddress;
        await containerRegistryRepository.UpdateAsync(registry, ct);

        if (!string.Equals(newAddress, oldAddress, StringComparison.Ordinal))
            await dockerRegistryService.LogoutAsync(oldAddress, ct);

        await SendOkAsync(Map.FromEntity(registry), ct);
    }
}

