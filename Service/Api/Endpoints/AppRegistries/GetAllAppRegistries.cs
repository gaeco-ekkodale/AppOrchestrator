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
/// Returns all configured application registries.
/// </summary>
public class GetAllAppRegistries(IAppRegistryRepository appRegistryRepository)
    : EndpointWithoutRequest<IEnumerable<AppRegistryDTO>, AppRegistryMapper>
{
    public override void Configure()
    {
        Get("app-registries");
        Summary(s =>
        {
            s.Summary = "List application registries.";
            s.Description = "Returns all stored registry definitions including metadata such as creation time and linked stack count. This endpoint is typically used to populate selection lists for deployment workflows.";
            s.Response<IEnumerable<AppRegistryDTO>>(200, "A full list of registries sorted by repository implementation defaults.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var registries = await appRegistryRepository.ListAsync(ct);
        await SendOkAsync(registries.Select(Map.FromEntity), ct);
    }
}
