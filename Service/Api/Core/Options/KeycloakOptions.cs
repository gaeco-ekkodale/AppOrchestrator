// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Core.Options;

/// <summary>
/// Configuration options for Keycloak authentication.
/// Contains settings required to connect to and authenticate with the Keycloak identity server.
/// </summary>
public class KeycloakOptions
{
    /// <summary>
    /// Gets the configuration section name for Keycloak settings.
    /// </summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Gets or sets the Keycloak server host URL.
    /// </summary>
    /// <example>https://keycloak.example.com</example>
    public required string Host { get; set; }

    /// <summary>
    /// Gets or sets the Keycloak realm name.
    /// A realm manages a set of users, credentials, roles, and groups.
    /// </summary>
    /// <example>my-realm</example>
    public required string Realm { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2/OIDC client identifier.
    /// </summary>
    /// <example>app-registry-api</example>
    public required string ClientId { get; set; }
}