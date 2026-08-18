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

public sealed class StackDetailsMapper : ResponseMapper<StackDetailsDTO, Stack>
{
    public override StackDetailsDTO FromEntity(Stack e)
    {
        var rs = e as RegistryStack;
        return new StackDetailsDTO
        {
            StackName = e.StackName,
            DockerProjectName = e.DockerProjectName,
            NetworkName = e.NetworkName,
            AppRegistryId = rs?.AppRegistryId,
            AppRegistryName = rs?.AppRegistry?.Name,
            PackageId = rs?.PackageId,
            PackageVersion = rs?.PackageVersion,
            Status = StackStatus.Unknown,
            Source = e is RegistryStack ? StackSource.AppStore : StackSource.CustomCompose,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
