// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.AppRegistries;

/// <summary>
/// Deletes an application registry by identifier.
/// </summary>
public class DeleteAppRegistry(IAppRegistryRepository appRegistryRepository)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("app-registries/{id}");
        Summary(s =>
        {
            s.Summary = "Delete application registry.";
            s.Description = "Removes a registry definition from persistent storage. Existing stacks referencing the registry are subject to repository and database constraints.";
            s.Response(204, "The registry was deleted successfully.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No registry exists for the provided id.");
            s.Response(409, "The registry is still referenced by one or more stacks.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var registry = await appRegistryRepository.GetByIdAsync(id, ct);
        if (registry is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (registry.Stacks.Count > 0)
        {
            ThrowError("Registry is still referenced by stacks. Remove or reassign stacks before deleting the registry.", 409);
            return;
        }

        await appRegistryRepository.DeleteAsync(id, ct);
        await SendNoContentAsync(ct);
    }
}
