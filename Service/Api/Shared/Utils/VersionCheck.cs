// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Models;

namespace AppOrchestrator.Api.Shared.Utils;

public static class VersionCheck
{
    public static bool IsVersionAllowed(string version, List<AllowedVersionSuffix> allowedSuffixes)
    {
        if (allowedSuffixes.Count == 0) return true;
        var dashIndex = version.IndexOf('-');
        var preRelease = dashIndex >= 0 ? version[(dashIndex + 1)..] : "";
        return allowedSuffixes.Any(s =>
            s.Suffix == ""
                ? dashIndex < 0
                : preRelease.Equals(s.Suffix, StringComparison.OrdinalIgnoreCase));
    }
}