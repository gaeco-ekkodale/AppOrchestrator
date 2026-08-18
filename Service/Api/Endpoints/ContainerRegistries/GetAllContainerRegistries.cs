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

namespace AppOrchestrator.Api.Endpoints.ContainerRegistries;

/// <summary>
/// Returns all configured container registries.
/// </summary>
public class GetAllContainerRegistries(IContainerRegistryRepository containerRegistryRepository)
    : EndpointWithoutRequest<IEnumerable<ContainerRegistryDTO>, ContainerRegistryMapper>
{
    public override void Configure()
    {
        Get("container-registries");
        Summary(s =>
        {
            s.Summary = "List container registries.";
            s.Description = "Returns all configured container registries that can be used for image pulls. Use this endpoint to populate registry selection in deployment clients.";
            s.Response<IEnumerable<ContainerRegistryDTO>>(200, "A list of container registry records without credential material.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var registries = await containerRegistryRepository.ListAsync(ct);
        await SendOkAsync(registries.Select(Map.FromEntity), ct);
    }
}
