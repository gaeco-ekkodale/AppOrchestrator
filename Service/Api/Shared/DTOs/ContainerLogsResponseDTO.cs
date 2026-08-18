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
/// Cursor-based container log response for polling clients.
/// </summary>
public class ContainerLogsResponseDTO
{
    /// <summary>Container id requested by the client.</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Cursor for the next poll request. Pass this value back as 'since'.
    /// </summary>
    public string NextSince { get; set; } = string.Empty;

    /// <summary>Returned log lines ordered by timestamp ascending.</summary>
    public List<ContainerLogLineDTO> Lines { get; set; } = [];
}
