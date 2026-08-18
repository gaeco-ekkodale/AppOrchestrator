// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Shared.DTOs;

/// <summary>
/// Detailed stack response including resolved environment configuration and live container state.
/// </summary>
public class StackDetailsDTO : StackDTO
{
    /// <summary>
    /// Effective environment variables represented as key-value pairs.
    /// </summary>
    public Dictionary<string, string>? EnvConfig { get; set; } = [];

}
