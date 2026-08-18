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

public class GetNetworkRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Returns a single network by name.
/// </summary>
public class GetNetwork(INetworkRepository networkRepository)
    : Endpoint<GetNetworkRequest, NetworkDTO, NetworkMapper>
{
    public override void Configure()
    {
        Get("networks/{name}");
        Summary(s =>
        {
            s.Summary = "Get network.";
            s.Description = "Returns the network with the specified name, including its assigned stack summaries.";
            s.Response<NetworkDTO>(200, "Network found.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Network not found.");
        });
    }

    public override async Task HandleAsync(GetNetworkRequest req, CancellationToken ct)
    {
        var network = await networkRepository.GetByNameAsync(req.Name, ct);
        if (network is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(Map.FromEntity(network), 200, ct);
    }
}
