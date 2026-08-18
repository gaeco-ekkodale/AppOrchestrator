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

namespace AppOrchestrator.Api.Services._Interfaces.Stacks;

/// <summary>
/// Holds the editable compose and environment content read from a stack workspace.
/// Returned by deployment operations that need to expose file contents to callers.
/// </summary>
/// <param name="StackName">User-facing display name of the stack.</param>
/// <param name="ComposeContent">Full text content of <c>docker-compose.yml</c>.</param>
/// <param name="EnvConfig">Key-value pairs parsed from the workspace <c>.env</c> file.</param>
public record StackComposeData(
    string StackName,
    string ComposeContent,
    Dictionary<string, string> EnvConfig);

/// Command to deploy a new stack from a versioned package in an application registry.
/// </summary>
/// <param name="StackName">Desired human-readable name for the new stack.</param>
/// <param name="RegistryId">Identifier of the <see cref="AppRegistry"/> to fetch the compose file from.</param>
/// <param name="PackageId">Package identifier within the registry, e.g. <c>my-app</c>.</param>
/// <param name="Version">Package version to deploy, e.g. <c>1.2.3</c>.</param>
/// <param name="EnvConfig">Key-value environment variables written to the workspace <c>.env</c> file.</param>
/// <param name="NetworkName">Name of an orchestrator-managed network to connect the stack to.</param>
public record CreateStackFromRegistryCommand(
    string StackName,
    Guid RegistryId,
    string PackageId,
    string Version,
    Dictionary<string, string> EnvConfig,
    string NetworkName);

/// <summary>
/// Command to deploy a new custom stack from caller-supplied compose content.
/// </summary>
/// <param name="StackName">Desired human-readable name for the new stack.</param>
/// <param name="ComposeContent">Full <c>docker-compose.yml</c> content provided by the caller.</param>
/// <param name="EnvConfig">Key-value environment variables written to the workspace <c>.env</c> file.</param>
/// <param name="NetworkName">Name of an orchestrator-managed network to connect the stack to.</param>
public record CreateCustomStackCommand(
    string StackName,
    string ComposeContent,
    Dictionary<string, string> EnvConfig,
    string NetworkName);

/// <summary>
/// Command to clone an existing stack into a new one, copying its complete workspace.
/// At least one of <paramref name="StackName"/> or <paramref name="NetworkName"/> must lead to a
/// project name that differs from the source, since the project name is derived from both.
/// </summary>
/// <param name="SourceProjectName">Docker Compose project name of the stack to clone.</param>
/// <param name="StackName">Display name for the clone; <c>null</c> to keep the source name.</param>
/// <param name="NetworkName">
/// Network for the clone; <c>null</c> to keep the source network.
/// Pass an empty string <c>""</c> to create the clone detached from any network.
/// </param>
public record CloneStackCommand(
    string SourceProjectName,
    string? StackName,
    string? NetworkName);

/// <summary>
/// Command to apply partial updates to an existing stack (rename, version bump, env-only update).
/// Omit a field by leaving it <c>null</c> to skip that update.
/// </summary>
/// <param name="ProjectName">Docker Compose project name of the stack to update.</param>
/// <param name="StackName">New display name; <c>null</c> to keep the current name.</param>
/// <param name="Version">New package version to deploy; <c>null</c> to keep the current version.</param>
/// <param name="EnvConfig">
/// Replacement env key-value map; <c>null</c> to keep the current env file untouched
/// (unless a version update is requested, in which case the current env is reused).
/// </param>
/// <param name="NetworkName">
/// New network assignment by name; <c>null</c> to keep the current assignment.
/// Pass an empty string <c>""</c> to explicitly detach the stack from any network.
/// </param>
public record UpdateStackCommand(
    string ProjectName,
    string? StackName,
    string? Version,
    Dictionary<string, string>? EnvConfig,
    string? NetworkName);

/// <summary>
/// Command to replace the compose file and environment of a custom stack and
/// re-deploy it in-place.
/// </summary>
/// <param name="ProjectName">Docker Compose project name of the custom stack to update.</param>
/// <param name="ComposeContent">New <c>docker-compose.yml</c> content to write to the workspace.</param>
/// <param name="EnvConfig">New environment key-value map to write to <c>.env</c>.</param>
public record UpdateStackComposeCommand(
    string ProjectName,
    string ComposeContent,
    Dictionary<string, string> EnvConfig);

/// <summary>
/// Orchestrates the full lifecycle of stack deployments: creating stacks from registry
/// packages or custom compose content, updating running stacks (rename, version, env),
/// and cloning existing stacks.
///
/// Each operation coordinates multiple infrastructure concerns — HTTP registry access,
/// workspace file management, Docker Compose execution, and database persistence — and
/// encapsulates the associated rollback and backup logic. Endpoints should call this
/// service for any operation that requires combining more than one of those concerns.
/// </summary>
public interface IStackDeploymentService
{
    /// <summary>
    /// Deploys a new stack by fetching <c>docker-compose.yml</c> for the specified package
    /// version from the referenced application registry, writing workspace files, executing
    /// <c>docker compose up -d</c>, and persisting the stack entity.
    /// </summary>
    /// <param name="command">Parameters for the registry-based deployment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted <see cref="Stack"/> entity including its generated ID.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the referenced registry does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a stack with the derived project name already exists.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the registry endpoint is unreachable.</exception>
    Task<Stack> CreateFromRegistryAsync(CreateStackFromRegistryCommand command, CancellationToken ct = default);

    /// <summary>
    /// Deploys a new custom stack from caller-supplied compose content, writes workspace files,
    /// executes <c>docker compose up -d</c>, and persists the stack entity.
    /// </summary>
    /// <param name="command">Parameters for the custom deployment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted <see cref="Stack"/> entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a stack with the derived project name already exists.</exception>
    Task<Stack> CreateCustomAsync(CreateCustomStackCommand command, CancellationToken ct = default);

    /// <summary>
    /// Creates a new stack as a full copy of an existing one: the entire source workspace is
    /// duplicated, including package files and volume data, so the clone starts from identical
    /// state. The clone keeps the source's registry/package metadata and is not started.
    /// </summary>
    /// <param name="command">Clone target name and/or network.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted <see cref="Stack"/> entity for the clone.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the source stack does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when neither name nor network differs from the source.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a stack with the derived project name already exists.</exception>
    Task<Stack> CloneAsync(CloneStackCommand command, CancellationToken ct = default);

    /// <summary>
    /// Applies partial updates to an existing stack. Supported operations:
    /// rename (workspace + DB), version update (fetch new compose + backup/restore on failure),
    /// and env-only update (rewrite <c>.env</c> + re-apply compose).
    /// At least one of <see cref="UpdateStackCommand.StackName"/>,
    /// <see cref="UpdateStackCommand.Version"/>, or <see cref="UpdateStackCommand.EnvConfig"/> must be non-null.
    /// </summary>
    /// <param name="command">Partial update command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated and re-fetched <see cref="Stack"/> entity.</returns>
    /// <exception cref="ArgumentException">Thrown when no updateable fields are provided, or a version update is requested on a custom stack.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the stack or its linked registry does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown on rename conflict, or renaming a running stack.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the registry is unreachable during a version update.</exception>
    Task<Stack> UpdateAsync(UpdateStackCommand command, CancellationToken ct = default);

    /// <summary>
    /// Replaces the <c>docker-compose.yml</c> and <c>.env</c> of a custom stack in-place,
    /// re-deploys via <c>docker compose up -d</c>, and updates the DB timestamp.
    /// Only supported for stacks that are not linked to a registry.
    /// </summary>
    /// <param name="command">New compose content and env configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="StackComposeData"/> with the final persisted compose and env content.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the stack does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the stack is registry-managed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <c>docker compose up</c> fails after writing the new files.</exception>
    Task<StackComposeData> UpdateComposeAsync(UpdateStackComposeCommand command, CancellationToken ct = default);

    /// <summary>
    /// Stops and removes all Docker resources of the stack, deletes the workspace directory,
    /// and removes the database record. Safe to call even when the stack does not exist in the DB.
    /// </summary>
    /// <param name="projectName">Docker Compose project name of the stack to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string projectName, CancellationToken ct = default);
}
