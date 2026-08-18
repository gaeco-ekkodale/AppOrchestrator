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
using AppOrchestrator.Domain.Repositories;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Endpoints.AppRegistries;

/// <summary>
/// Request payload for partial updates of an existing application registry.
/// </summary>
public class UpdateAppRegistryRequest
{
    /// <summary>
    /// Optional new display name of the registry.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional new base URL of the registry API.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Optional API key update.
    /// null  → leave the stored key unchanged
    /// ""    → remove the stored key
    /// other → encrypt and replace the stored key
    /// </summary>
    public string? ApiKey { get; set; }
}

public class UpdateAppRegistryValidator : Validator<UpdateAppRegistryRequest>
{
    public UpdateAppRegistryValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty if provided.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
        });

        When(x => x.BaseUrl is not null, () =>
        {
            RuleFor(x => x.BaseUrl)
                .NotEmpty().WithMessage("Base URL cannot be empty if provided.")
                .MaximumLength(500).WithMessage("Base URL cannot exceed 500 characters.")
                .Matches(@"^https?://").WithMessage("Base URL must start with http:// or https://");
        });

        When(x => x.ApiKey is not null && x.ApiKey.Length > 0, () =>
        {
            RuleFor(x => x.ApiKey)
                .MaximumLength(500).WithMessage("API key cannot exceed 500 characters.");
        });
    }
}

/// <summary>
/// Applies partial updates to an existing application registry.
/// </summary>
public class UpdateAppRegistry(IAppRegistryRepository appRegistryRepository, IRegistrySecretProtector secretProtector)
    : Endpoint<UpdateAppRegistryRequest, AppRegistryDTO, AppRegistryMapper>
{
    public override void Configure()
    {
        Put("app-registries/{id}");
        Summary(s =>
        {
            s.Summary = "Update an application registry.";
            s.Description = "Performs a partial update. Only fields present in the request are changed. The endpoint returns the complete resulting state after persistence.";
            s.Response<AppRegistryDTO>(200, "The registry was updated and returned in its final persisted state.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(404, "No registry exists for the provided id.");
            s.Response(409, "Another registry already uses the requested base URL.");
        });
    }

    public override async Task HandleAsync(UpdateAppRegistryRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var registry = await appRegistryRepository.GetByIdAsync(id, ct);
        if (registry is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (req.Name is not null)
            registry.Name = req.Name;

        if (req.BaseUrl is not null && !string.Equals(req.BaseUrl, registry.BaseUrl, StringComparison.Ordinal))
        {
            var existing = await appRegistryRepository.GetByBaseUrlAsync(req.BaseUrl, ct);
            if (existing is not null && existing.Id != registry.Id)
            {
                ThrowError("An app registry with this base URL already exists.", 409);
                return;
            }
            registry.BaseUrl = req.BaseUrl;
        }

        if (req.ApiKey is not null)
        {
            registry.ApiKeyEncrypted = req.ApiKey.Length > 0
                ? secretProtector.Protect(req.ApiKey)
                : null;
        }

        await appRegistryRepository.UpdateAsync(registry, ct);
        await SendOkAsync(Map.FromEntity(registry), ct);
    }
}

