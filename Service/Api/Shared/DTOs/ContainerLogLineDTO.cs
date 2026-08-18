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
/// One log line emitted by a container stream.
/// </summary>
public class ContainerLogLineDTO
{
    /// <summary>UTC timestamp in <c>yyyy-MM-dd HH:mm:ss</c> format.</summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>Log source stream: stdout or stderr.</summary>
    public string Stream { get; set; } = string.Empty;

    /// <summary>Raw log message content without timestamp prefix.</summary>
    public string Message { get; set; } = string.Empty;
}
