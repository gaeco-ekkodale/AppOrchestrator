// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AppOrchestrator.Api.Services._Interfaces.Storage;

/// <summary>
/// Provides all file and workspace directory operations used across the application.
/// Centralises path computation, .env serialisation and directory management.
/// </summary>
public interface IFileService
{
    // Workspace path operations.

    /// <summary>Returns the absolute workspace directory for a given project (used for file I/O).
    /// Computed as {RootPath}/stacks/{projectName}.</summary>
    string GetInternalWorkspacePath(string projectName);

    /// <summary>Lists all project folder names under the stacks workspace root.</summary>
    IReadOnlyList<string> ListWorkspaceProjectNames();

    // Directory operations.

    /// <summary>Creates the directory (and any missing parents).</summary>
    void CreateDirectory(string path);

    /// <summary>Returns true when the directory exists on disk.</summary>
    bool DirectoryExists(string path);

    /// <summary>Deletes the directory and all its contents recursively.</summary>
    void DeleteDirectory(string path);

    /// <summary>Moves (renames) a directory. Destination must not exist yet.</summary>
    void MoveDirectory(string source, string destination);

    /// <summary>Recursively copies the directory tree from <paramref name="source"/> into <paramref name="destination"/>.
    /// Existing files in the destination are overwritten when <paramref name="overwrite"/> is true.</summary>
    void CopyDirectory(string source, string destination, bool overwrite = false);

    // Stack file operations.

    /// <summary>Writes the compose stream to docker-compose.yml inside the workspace.</summary>
    Task WriteComposeFileAsync(string workspacePath, Stream content, CancellationToken ct = default);

    /// <summary>Reads the docker-compose.yml file from the workspace. Returns an empty string when the file does not exist.</summary>
    Task<string> ReadComposeFileAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>Serialises the env dictionary to a .env file inside the workspace.</summary>
    Task WriteEnvFileAsync(string workspacePath, Dictionary<string, string> envConfig, CancellationToken ct = default);

    /// <summary>Reads and parses the .env file from the workspace. Returns an empty dictionary when the file does not exist.</summary>
    Task<Dictionary<string, string>> ReadEnvFileAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Resolves the host-side workspace path for a project by inspecting the container's own
    /// Docker bind mounts. Docker needs host paths as bind-mount sources, not container paths.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when running inside a container but the host path cannot be resolved.
    /// </exception>
    Task<string> GetHostWorkspacePath(string projectName, CancellationToken ct = default);

    /// <summary>
    /// Unpacks a registry package ZIP into the workspace:
    /// the files declared under <c>packageFiles</c> in its manifest replace
    /// {workspacePath}/package_files/, while manifest, env schema and icon land next to the compose
    /// file so they stay available without the registry. Everything else is ignored.
    /// Pass <c>null</c> to leave the workspace untouched.
    /// </summary>
    Task ExtractPackageFilesAsync(string workspacePath, Stream? zipStream, CancellationToken ct = default);

    Task DeleteVolumes(string projectName);
}
