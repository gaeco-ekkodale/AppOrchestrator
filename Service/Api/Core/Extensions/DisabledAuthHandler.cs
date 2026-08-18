// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Core.Extensions;

/// <summary>
/// Authentication handler used when authentication is disabled (Auth:Enabled=false).
/// It authenticates every request as a synthetic local admin (group "/Admin"), so the
/// existing "AdminOnly" policy and the default "Bearer" auth-scheme requirement pass
/// without a real Keycloak token.
///
/// Intended for local bootstrap only: the orchestrator must come up and be usable
/// (register registries, create networks, deploy the base) BEFORE Keycloak itself is
/// deployed. Never enable this in a real/shared environment.
/// </summary>
public class DisabledAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "local-admin"),
            new Claim("groups", "/Admin"),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
