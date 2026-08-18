// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.Extensions.Hosting;

namespace AppOrchestrator.Api.Services._Interfaces.Mfe;

/// <summary>
/// Hosted background listener for Docker container lifecycle events that drive
/// micro-frontend enable/disable synchronization with the shell host.
/// </summary>
public interface IDockerEventListener : IHostedService;
