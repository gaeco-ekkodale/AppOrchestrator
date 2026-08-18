// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces;
using AppOrchestrator.Api.Shared.DTOs;
using AppOrchestrator.Api.Shared.Mappers;
using AppOrchestrator.Domain.Models;
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.AppRegistries;


/// <summary>
/// Request payload for creating a new application registry.
/// </summary>
public class CreateAppRegistryRequest
{
    /// <summary>
    /// Human-readable registry name used in UI and API responses.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the registry API, for example https://registry.company.local.
    /// The URL must uniquely identify one registry instance.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional API key used by the orchestrator to authenticate against the registry.
    /// The key is stored encrypted and is never returned in responses.
    /// </summary>
    public string? ApiKey { get; set; }
}

public class CreateAppRegistryValidator : Validator<CreateAppRegistryRequest>
{
    public CreateAppRegistryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);
        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("Base URL is required.")
            .MaximumLength(500)
            .Matches(@"^https?://").WithMessage("Base URL must start with http:// or https://");

        When(x => x.ApiKey is not null, () =>
        {
            RuleFor(x => x.ApiKey)
                .NotEmpty().WithMessage("API key cannot be empty if provided.")
                .MaximumLength(500).WithMessage("API key cannot exceed 500 characters.");
        });
    }
}


/// <summary>
/// Creates a persistent application registry entry used to resolve deployable packages.
/// </summary>
public class CreateAppRegistry(IAppRegistryRepository appRegistryRepository, IRegistrySecretProtector secretProtector)
    : Endpoint<CreateAppRegistryRequest, AppRegistryDTO, AppRegistryMapper>
{
    public override void Configure()
    {
        Post("app-registries");
        Summary(s =>
        {
            s.Summary = "Create an application registry.";
            s.Description = "Stores a new registry definition that can later be referenced during stack deployments. The base URL must be unique because it is the routing key used to fetch package metadata and compose files.";
            s.Response<AppRegistryDTO>(201, "The registry was created and returned with generated identifiers and timestamps.");
            s.Response(400, "The request payload is invalid, for example missing required fields.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(409, "Another registry already uses the same base URL.");
        });
    }

    public override async Task HandleAsync(CreateAppRegistryRequest req, CancellationToken ct)
    {
        var existing = await appRegistryRepository.GetByBaseUrlAsync(req.BaseUrl, ct);
        if (existing is not null)
        {
            ThrowError("An app registry with this base URL already exists.", 409);
            return;
        }

        var registry = new AppRegistry
        {
            Name = req.Name,
            BaseUrl = req.BaseUrl,
            CreatedAt = DateTime.UtcNow,
            ApiKeyEncrypted = !string.IsNullOrEmpty(req.ApiKey)
                ? secretProtector.Protect(req.ApiKey)
                : null
        };

        await appRegistryRepository.AddAsync(registry, ct);
        await SendAsync(Map.FromEntity(registry), 201, ct);
    }
}

