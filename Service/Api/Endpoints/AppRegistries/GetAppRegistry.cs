// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;

namespace AppOrchestrator.Api.Endpoints.AppRegistries;

/// <summary>
/// Loads one application registry by identifier.
/// </summary>
public class GetAppRegistry(IAppRegistryRepository appRegistryRepository)
    : EndpointWithoutRequest<AppRegistryDTO, AppRegistryMapper>
{
    public override void Configure()
    {
        Get("app-registries/{id}");
        Summary(s =>
        {
            s.Summary = "Get application registry by id.";
            s.Description = "Retrieves one registry record including current metadata. Useful for edit forms and detailed registry views.";
            s.Response<AppRegistryDTO>(200, "The requested registry was found and returned.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No registry exists for the provided id.");
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

        await SendOkAsync(Map.FromEntity(registry), ct);
    }
}
