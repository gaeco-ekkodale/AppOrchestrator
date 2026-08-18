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
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.ContainerRegistries;

/// <summary>
/// Deletes a container registry entry and tries to remove Docker login state.
/// </summary>
public class DeleteContainerRegistry(
    IContainerRegistryRepository containerRegistryRepository,
    IDockerRegistryService dockerRegistryService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("container-registries/{id}");
        Summary(s =>
        {
            s.Summary = "Delete container registry.";
            s.Description = "Removes a registry definition by id. The endpoint also triggers docker logout for the stored server address as a cleanup step.";
            s.Response(204, "Registry was deleted. Logout is attempted before removal.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No registry exists for the provided id.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var registry = await containerRegistryRepository.GetByIdAsync(id, ct);
        if (registry is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await dockerRegistryService.LogoutAsync(registry.ServerAddress, ct);
        await containerRegistryRepository.DeleteAsync(id, ct);
        await SendNoContentAsync(ct);
    }
}
