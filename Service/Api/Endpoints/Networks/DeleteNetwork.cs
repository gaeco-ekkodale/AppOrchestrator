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

namespace AppOrchestrator.Api.Endpoints.Networks;

public class DeleteNetworkRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Removes a user-created Docker network and deletes the database record.
///
/// Workflow:
/// 1. Load the entity from DB
/// 2. Delete the network from Docker (no-op if already gone)
/// 3. Delete the DB record
/// </summary>
public class DeleteNetwork(
    INetworkRepository networkRepository,
    IDockerNetworkService dockerNetworkService)
    : Endpoint<DeleteNetworkRequest>
{
    public override void Configure()
    {
        Delete("networks/{name}");
        Summary(s =>
        {
            s.Summary = "Delete network.";
            s.Description = "Removes the Docker network from the daemon by name and deletes the database record. If the network no longer exists in Docker a warning is logged and the DB record is still removed.";
            s.Response(204, "Network removed from Docker and database.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "Network not found in database.");
            s.Response(409, "Network still has containers attached and cannot be deleted.");
        });
    }

    public override async Task HandleAsync(DeleteNetworkRequest req, CancellationToken ct)
    {
        var network = await networkRepository.GetByNameAsync(req.Name, ct);
        if (network is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (await dockerNetworkService.HasContainersAsync(network.Name, ct))
            ThrowError($"Network '{network.Name}' still has containers attached. Stop all containers using this network before deleting it.", 409);

        await dockerNetworkService.DeleteNetworkAsync(network.Name, ct);

        await networkRepository.DeleteAsync(network.Name, ct);
        await SendNoContentAsync(ct);
    }
}
