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
using AppOrchestrator.Domain.Models;
using FastEndpoints;

namespace AppOrchestrator.Api.Shared.Mappers;

public sealed class ContainerRegistryMapper : ResponseMapper<ContainerRegistryDTO, ContainerRegistry>
{
    public override ContainerRegistryDTO FromEntity(ContainerRegistry e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        ServerAddress = e.ServerAddress,
        CreatedAt = e.CreatedAt,
    };
}
