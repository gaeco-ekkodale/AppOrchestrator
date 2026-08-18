// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces.Mfe;

/// <summary>
/// Docker label keys used for MFE plugins and host auto-discovery.
/// </summary>
public static class MfeLabels
{
    public const string Id = "app.mfe.id";
    public const string DisplayName = "app.mfe.displayName";
    public const string Description = "app.mfe.description";
    public const string IconPath = "app.mfe.iconPath";
    public const string EntrypointPath = "app.mfe.entrypointPath";
    public const string ExposedModule = "app.mfe.exposedModule";
    public const string Route = "app.mfe.route";

    public const string HostEnabled = "orchestrator.host";
    public const string HostApiKey = "orchestrator.apiKey";

    public static readonly string[] PluginKeys =
    [
        Id,
        DisplayName,
        Description,
        IconPath,
        EntrypointPath,
        ExposedModule,
        Route
    ];

    public static bool IsPluginLabel(string key) =>
        PluginKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
}
