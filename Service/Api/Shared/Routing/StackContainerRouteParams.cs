// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FastEndpoints;

namespace AppOrchestrator.Api.Shared.Routing;

public class StackContainerRouteParams : StackRouteParams
{
    [BindFrom("containerId")]
    public string ContainerId { get; set; } = string.Empty;
}

public abstract class StackContainerRouteValidator<TRequest> : Validator<TRequest>
    where TRequest : StackContainerRouteParams
{
    protected StackContainerRouteValidator()
    {
        //RuleFor(x => x.ContainerId)
        //    .Must(ContainerId.IsValid)
        //    .WithMessage("Route parameter 'containerId' must start with 'orch-' and contain only lowercase alphanumeric characters and hyphens.");
    }
}

public class StackContainerRouteParamsValidator : StackContainerRouteValidator<StackContainerRouteParams>;