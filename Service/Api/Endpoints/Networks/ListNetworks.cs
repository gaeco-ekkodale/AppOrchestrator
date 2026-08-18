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

namespace AppOrchestrator.Api.Endpoints.Networks;

/// <summary>
/// Returns all user-created networks with their assigned stack summaries.
/// </summary>
public class ListNetworks(INetworkRepository networkRepository)
    : EndpointWithoutRequest<IEnumerable<NetworkDTO>, NetworkMapper>
{
    public override void Configure()
    {
        Get("networks");
        Summary(s =>
        {
            s.Summary = "List networks.";
            s.Description = "Returns all user-created Docker networks stored in the database, including a summary of stacks assigned to each network.";
            s.Response<IEnumerable<NetworkDTO>>(200, "List of networks.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var networks = await networkRepository.ListAsync(ct);
        await SendAsync(networks.Select(Map.FromEntity), 200, ct);
    }
}
