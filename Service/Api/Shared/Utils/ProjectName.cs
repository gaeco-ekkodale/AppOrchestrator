// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Text.RegularExpressions;

namespace AppOrchestrator.Api.Shared.Utils;

public static class ProjectName
{
    public const string Prefix = "orch";
    public const string Pattern = "^[a-z0-9-]+$";

    private static readonly Regex ProjectNameRegex = new(Pattern, RegexOptions.Compiled);
    private static readonly Regex InvalidSlugCharsRegex = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) && ProjectNameRegex.IsMatch(value);

    public static string EnsureValid(string value, string paramName = "projectName")
    {
        if (!IsValid(value))
            throw new ArgumentException($"Invalid project name '{value}'. Must start with '{Prefix}' and contain only lowercase alphanumeric characters and hyphens.", paramName);

        return value;
    }

    public static string FromStackName(string stackName, string networkName)
    {
        if (string.IsNullOrWhiteSpace(stackName))
            throw new ArgumentException("Stack name must not be empty.", nameof(stackName));

        var slug = InvalidSlugCharsRegex
            .Replace(stackName.ToLowerInvariant(), "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Stack name must contain at least one alphanumeric character.", nameof(stackName));

        return $"{Prefix}-{networkName}-{slug}";
    }
}