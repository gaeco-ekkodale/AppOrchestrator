// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentValidation;

namespace AppOrchestrator.Api.Core.Options.Validators;

/// <summary>
/// Validator for Keycloak configuration options.
/// Ensures all required Keycloak settings are properly configured.
/// </summary>
public class KeycloakOptionsValidator : AbstractValidator<KeycloakOptions>
{
    public KeycloakOptionsValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty()
            .WithMessage("Keycloak host is required");

        RuleFor(x => x.Realm)
            .NotEmpty()
            .WithMessage("Keycloak realm is required");

        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("Keycloak client ID is required");
    }
}