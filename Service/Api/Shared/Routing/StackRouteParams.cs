// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Shared.Utils;
using FastEndpoints;
using FluentValidation;

namespace AppOrchestrator.Api.Shared.Routing;

public class StackRouteParams
{
    [BindFrom("projectName")]
    public string ProjectName { get; set; } = string.Empty;
}

public abstract class StackRouteValidator<TRequest> : Validator<TRequest>
    where TRequest : StackRouteParams
{
    protected StackRouteValidator()
    {
        RuleFor(x => x.ProjectName)
            .Must(ProjectName.IsValid)
            .WithMessage("Route parameter 'projectName' must contain only lowercase alphanumeric characters and hyphens.");
    }
}

public class StackRouteParamsValidator : StackRouteValidator<StackRouteParams>;