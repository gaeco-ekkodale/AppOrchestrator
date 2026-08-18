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

public sealed class AppRegistryMapper : ResponseMapper<AppRegistryDTO, AppRegistry>
{
    public override AppRegistryDTO FromEntity(AppRegistry e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        BaseUrl = e.BaseUrl,
        CreatedAt = e.CreatedAt,
        StackCount = e.Stacks?.Count ?? 0,
        HasApiKey = !string.IsNullOrEmpty(e.ApiKeyEncrypted)
    };
}
