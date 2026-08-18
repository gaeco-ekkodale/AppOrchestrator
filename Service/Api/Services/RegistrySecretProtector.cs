// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AppOrchestrator.Api.Services;

/// <inheritdoc cref="IRegistrySecretProtector"/>
public class RegistrySecretProtector : IRegistrySecretProtector
{
    private readonly IDataProtector _protector;

    public RegistrySecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("AppRegistry.ApiKey");
    }

    /// <inheritdoc/>
    public string Protect(string plaintext) => _protector.Protect(plaintext);

    /// <inheritdoc/>
    public string? Unprotect(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return null;
        return _protector.Unprotect(ciphertext);
    }
}
