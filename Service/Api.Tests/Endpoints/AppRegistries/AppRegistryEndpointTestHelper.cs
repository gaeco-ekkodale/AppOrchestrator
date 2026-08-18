// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Shared.Mappers;
using System.Reflection;

namespace AppOrchestrator.Api.Tests.Endpoints.AppRegistries;

internal static class AppRegistryEndpointTestHelper
{
    public static void InitializeAppRegistryMapper<TEndpoint>(TEndpoint endpoint)
    {
        var mapper = new AppRegistryMapper();
        var currentType = endpoint!.GetType();

        while (currentType is not null)
        {
            var mapperField = currentType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType == typeof(AppRegistryMapper));

            if (mapperField is not null)
            {
                mapperField.SetValue(endpoint, mapper);
                return;
            }

            currentType = currentType.BaseType;
        }

        throw new InvalidOperationException("Could not initialize AppRegistryMapper for endpoint test.");
    }
}
