// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces;

/// <summary>
/// Protects and unprotects registry API keys using ASP.NET Core Data Protection.
/// </summary>
public interface IRegistrySecretProtector
{
    /// <summary>
    /// Encrypts a plaintext API key for database storage.
    /// </summary>
    string Protect(string plaintext);

    /// <summary>
    /// Decrypts an encrypted API key blob. Returns null if the input is null or empty.
    /// </summary>
    string? Unprotect(string? ciphertext);
}
