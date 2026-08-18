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

public sealed class NetworkMapper : ResponseMapper<NetworkDTO, Network>
{
    public override NetworkDTO FromEntity(Network e) => new()
    {
        Name = e.Name,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        EnvironmentVariables = e.EnvironmentVariables.Select(ev => new EnvironmentVariableDTO
        {
            Name = ev.Name,
            Value = ev.Value
        }).ToList(),
        AllowedVersionSuffixes = e.AllowedVersionSuffixes.Select(s => s.Suffix).ToList(),
        Stacks = e.Stacks.Select(s => new NetworkStackSummary
        {
            StackName = s.StackName,
            DockerProjectName = s.DockerProjectName
        }).ToList()
    };
}
