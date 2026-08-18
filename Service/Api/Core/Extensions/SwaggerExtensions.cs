// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Options;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Core.Extensions;

/// <summary>
/// Extension methods for configuring Swagger documentation.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Configures Swagger generation with authentication support for FastEndpoints.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureSwagger(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        // Only enable Swagger in Development or if explicitly enabled via config
        // This prevents exposing API details in production
        if (!env.IsDevelopment())
        {
            //return;
        }

        var opts = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

        services.SwaggerDocument(o =>
        {
            o.ShortSchemaNames = true;
            o.MaxEndpointVersion = 1;
            o.DocumentSettings = s =>
            {
                s.Version = "v0";
                s.Title = "App Orchestrator API";
                s.Description = "API for managing applications and plugins";

                // Determine the Keycloak host for Swagger UI (Browser access)
                // If running in Docker (host.docker.internal) and Development, rewrite to localhost for the browser
                var keycloakHost = opts.Host;
                if (env.IsDevelopment() && keycloakHost.Contains("host.docker.internal"))
                {
                    keycloakHost = keycloakHost.Replace("host.docker.internal", "localhost");
                }

                // Add OAuth2 security definition
                s.AddSecurity("oauth2", new NSwag.OpenApiSecurityScheme
                {
                    Type = NSwag.OpenApiSecuritySchemeType.OAuth2,
                    Description = "Keycloak OAuth2 Flow",
                    Flows = new NSwag.OpenApiOAuthFlows
                    {
                        Implicit = new NSwag.OpenApiOAuthFlow
                        {
                            AuthorizationUrl = $"{keycloakHost}/realms/{opts.Realm}/protocol/openid-connect/auth",
                            TokenUrl = $"{keycloakHost}/realms/{opts.Realm}/protocol/openid-connect/token",
                            Scopes = new Dictionary<string, string>
                            {
                            {"openid", "OpenID Connect Scope" }
                            }
                        }
                    }
                });

                s.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("oauth2"));
            };
        });
    }

    /// <summary>
    /// Configures Swagger UI with OAuth authentication support.
    /// </summary>
    /// <param name="app">The web application to configure</param>
    public static void UseSwaggerUIWithOAuth(this WebApplication app)
    {
        var opts = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value;

        app.UseSwaggerGen(uiConfig: u =>
        {
            u.PersistAuthorization = true;
            u.DocExpansion = "list";
            u.OAuth2Client = new NSwag.AspNetCore.OAuth2ClientSettings
            {
                ClientId = opts.ClientId,
                AppName = "App Orchestrator",
                UsePkceWithAuthorizationCodeGrant = true
            };
        });
    }
}