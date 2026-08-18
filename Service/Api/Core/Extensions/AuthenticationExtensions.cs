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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppOrchestrator.Api.Core.Extensions;

/// <summary>
/// Extensions for configuring authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures JWT authentication using Keycloak as the identity provider.
    /// When <c>Auth:Enabled</c> is <c>false</c> a permissive handler is registered
    /// instead (see <see cref="DisabledAuthHandler"/>) so the orchestrator can be used
    /// during local bootstrap before Keycloak exists.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Application configuration (reads <c>Auth:Enabled</c>)</param>
    public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Local bootstrap escape hatch. Registered under the same "Bearer" scheme the
        // endpoints require (see Program.cs: ep.AuthSchemes("Bearer")), so nothing else
        // needs to change.
        var authEnabled = configuration.GetValue("Auth:Enabled", true);
        if (!authEnabled)
        {
            Console.WriteLine("WARNING: Authentication is DISABLED (Auth:Enabled=false). Every request runs as a local admin.");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, DisabledAuthHandler>(
                    JwtBearerDefaults.AuthenticationScheme, _ => { });
            return;
        }

        var serviceProvider = services.BuildServiceProvider();
        var opts = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{opts.Host}/realms/{opts.Realm}";
                options.IncludeErrorDetails = true;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log the exception or handle it as needed
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };

                // Configure multiple audiences
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !env.IsDevelopment(),
                    ValidateIssuer = !env.IsDevelopment(),
                    ValidateLifetime = !env.IsDevelopment(),
                    ValidAudience = "account",
                    NameClaimType = "preferred_username",
                    RoleClaimType = "groups"
                };

                options.RequireHttpsMetadata = !env.IsDevelopment();
            });
    }

    /// <summary>
    /// Configures authorization policies for the application.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireClaim("groups", "/Admin"))
            .AddPolicy("UserOnly", policy => policy.RequireClaim("groups", "/User"));

    }
}