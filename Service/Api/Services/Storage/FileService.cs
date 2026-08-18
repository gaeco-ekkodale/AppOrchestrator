// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Core.Options;
using AppOrchestrator.Api.Services._Interfaces.Storage;
using AppOrchestrator.Api.Shared.Utils;
using Docker.DotNet;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AppOrchestrator.Api.Services.Storage;

/// <inheritdoc cref="IFileService"/>
public class FileService(IDockerClient dockerClient, ILogger<FileService> logger, IOptions<OrchestratorOptions> options) : IFileService
{
    private readonly string RootPath = options.Value.RootPath;
    private const string StacksFolder = "stacks";
    private const string PackageFilesFolder = "package_files";
    private const string ComposeFileName = "docker-compose.yml";
    private const string EnvFileName = ".env";
    private const string ManifestFileName = "manifest.yaml";
    private const string EnvSchemaFileName = ".env.schema.yaml";

    /// <summary>
    /// Package ZIP entries the registry names itself. Everything else is either a declared package
    /// file or, for versions published before packageFiles existed, treated as one.
    /// </summary>
    private static readonly string[] RegistryOwnedFileNames =
        [ManifestFileName, EnvSchemaFileName, "docker-compose.yaml", ComposeFileName, "readme.md"];

    // Workspace path operations.

    /// <inheritdoc/>
    public string GetInternalWorkspacePath(string projectName)
    {
        ProjectName.EnsureValid(projectName, nameof(projectName));

        return Path.Combine(RootPath, StacksFolder, projectName);
    }


    /// <inheritdoc/>
    public IReadOnlyList<string> ListWorkspaceProjectNames()
    {
        if (!Directory.Exists(Path.Combine(RootPath, StacksFolder))) return [];

        return Directory
            .GetDirectories(Path.Combine(RootPath, StacksFolder))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();
    }

    // Directory operations.

    /// <inheritdoc/>
    public void CreateDirectory(string path) =>
        Directory.CreateDirectory(path);

    /// <inheritdoc/>
    public bool DirectoryExists(string path) =>
        Directory.Exists(path);

    /// <inheritdoc/>
    public void DeleteDirectory(string path) =>
        Directory.Delete(path, recursive: true);

    /// <inheritdoc/>
    public void MoveDirectory(string source, string destination) =>
        Directory.Move(source, destination);

    /// <inheritdoc/>
    public void CopyDirectory(string source, string destination, bool overwrite = false)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite);

        foreach (var dir in Directory.GetDirectories(source))
        {
            var childName = Path.GetFileName(dir);
            var targetChild = Path.Combine(destination, childName);
            CopyDirectory(dir, targetChild, overwrite);
        }
    }

    // Stack file operations.

    /// <inheritdoc/>
    public async Task WriteComposeFileAsync(string workspacePath, Stream content, CancellationToken ct = default)
    {
        var composePath = Path.Combine(workspacePath, ComposeFileName);
        await using var fs = File.Create(composePath);
        await content.CopyToAsync(fs, ct);
    }

    /// <inheritdoc/>
    public async Task<string> ReadComposeFileAsync(
        string workspacePath,
        CancellationToken ct = default)
    {
        var composePath = Path.Combine(workspacePath, ComposeFileName);
        if (!File.Exists(composePath)) return string.Empty;
        return await File.ReadAllTextAsync(composePath, ct);
    }

    /// <inheritdoc/>
    public async Task WriteEnvFileAsync(
        string workspacePath,
        Dictionary<string, string> envConfig,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in envConfig)
        {
            var safeValue = value.Contains(' ') || value.Contains('"') || value.Contains('=')
                ? $"\"{value.Replace("\"", "\\\"")}\""
                : value;
            sb.AppendLine($"{key}={safeValue}");
        }
        await File.WriteAllTextAsync(Path.Combine(workspacePath, EnvFileName), sb.ToString(), Encoding.UTF8, ct);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> ReadEnvFileAsync(
        string workspacePath,
        CancellationToken ct = default)
    {
        var envPath = Path.Combine(workspacePath, EnvFileName);
        if (!File.Exists(envPath)) return new Dictionary<string, string>();
        var result = new Dictionary<string, string>();
        foreach (var line in await File.ReadAllLinesAsync(envPath, ct))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

            var idx = trimmed.IndexOf('=');
            if (idx <= 0) continue;

            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim().Trim('"');
            result[key] = value;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<string> GetHostWorkspacePath(string projectName, CancellationToken ct = default)
    {
        ProjectName.EnsureValid(projectName, nameof(projectName));

        // In Docker the hostname defaults to the short container ID. Its absence means we are not
        // containerised (local development), where container and host path are identical.
        string containerId;
        try
        {
            containerId = (await File.ReadAllTextAsync("/etc/hostname", ct)).Trim();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex,
                "Not running in a container - using '{Path}' as host path unchanged.",
                RootPath);
            return Path.Combine(RootPath, StacksFolder, projectName);
        }

        // Containerised: the container path is not a valid bind-mount source on the host, so an
        // unresolvable host path is a hard error rather than something to fall back from.
        try
        {
            var inspect = await dockerClient.Containers.InspectContainerAsync(containerId, ct);

            // Find the most-specific bind-mount whose destination is a prefix of the path.
            var mount = inspect.Mounts
                .Where(m => string.Equals(m.Type, "bind", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Destination.Length)
                .FirstOrDefault(m =>
                    RootPath.StartsWith(m.Destination, StringComparison.OrdinalIgnoreCase));

            if (mount is null)
                throw new InvalidOperationException(
                    $"No bind-mount of this container covers the workspace root '{RootPath}'. " +
                    $"Mount the workspace from the host.");

            var relative = RootPath[mount.Destination.Length..].TrimStart('/');
            var hostRootPath = string.IsNullOrEmpty(relative)
                ? mount.Source
                : $"{mount.Source.TrimEnd('/')}/{relative}";

            var hostWorkspacePath = Path.Combine(hostRootPath, StacksFolder, projectName);

            logger.LogDebug(
                "Resolved '{ContainerPath}' -> '{HostPath}' via mount '{Mount}'",
                RootPath, hostWorkspacePath, mount.Destination);

            return hostWorkspacePath;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Failed to resolve the host-side path for workspace root '{RootPath}'.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task ExtractPackageFilesAsync(string workspacePath, Stream? zipStream, CancellationToken ct = default)
    {
        if (zipStream is null)
            return;

        if (zipStream.CanSeek && zipStream.Length == 0)
        {
            logger.LogWarning("Package ZIP for '{Workspace}' is empty - no package files extracted.", workspacePath);
            return;
        }

        var filesDir = Path.Combine(workspacePath, PackageFilesFolder);

        // Wiped first so files a new version no longer ships stop being mountable.
        if (Directory.Exists(filesDir))
            Directory.Delete(filesDir, recursive: true);
        Directory.CreateDirectory(filesDir);

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        var declaredPackageFiles = await ReadDeclaredPackageFilesAsync(archive, ct);

        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var destPath = ResolveEntryTarget(entry, workspacePath, filesDir, declaredPackageFiles);
            if (destPath is null) continue;

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            await using var dest = File.Open(destPath, FileMode.Create, FileAccess.Write);
            await using var src = entry.Open();
            await src.CopyToAsync(dest, ct);
        }
    }

    /// <summary>
    /// Reads the <c>packageFiles</c> names declared in the archive's manifest. Returns <c>null</c>
    /// when the archive ships no manifest, which is the case for versions published before the
    /// declaration existed - those fall back to treating every non-registry file as a package file.
    /// </summary>
    private async Task<HashSet<string>?> ReadDeclaredPackageFilesAsync(ZipArchive archive, CancellationToken ct)
    {
        var manifestEntry = archive.GetEntry(ManifestFileName);
        if (manifestEntry is null)
        {
            logger.LogInformation("Package ZIP ships no {Manifest} - treating all non-registry files as package files.", ManifestFileName);
            return null;
        }

        await using var manifestStream = manifestEntry.Open();
        using var reader = new StreamReader(manifestStream);
        var manifest = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<PackageManifest>(await reader.ReadToEndAsync(ct));

        return (manifest?.PackageFiles ?? [])
            .Select(file => file.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Decides where an archive entry belongs: registry metadata next to the compose file, declared
    /// package files under <c>package_files/</c>, everything else nowhere. Returns <c>null</c> for
    /// entries that are not extracted.
    /// </summary>
    private string? ResolveEntryTarget(
        ZipArchiveEntry entry,
        string workspacePath,
        string filesDir,
        HashSet<string>? declaredPackageFiles)
    {
        // Manifest, env schema and icon stay readable without the registry, so the deploy form
        // still works while it is unreachable.
        if (entry.Name.Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase) ||
            entry.Name.Equals(EnvSchemaFileName, StringComparison.OrdinalIgnoreCase) ||
            entry.Name.StartsWith("icon.", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(workspacePath, entry.Name);

        var isPackageFile = declaredPackageFiles is null
            ? !RegistryOwnedFileNames.Contains(entry.Name, StringComparer.OrdinalIgnoreCase)
            : declaredPackageFiles.Contains(entry.FullName) || declaredPackageFiles.Contains(entry.Name);

        if (!isPackageFile) return null;

        var destPath = Path.GetFullPath(Path.Combine(filesDir, entry.FullName));
        if (!destPath.StartsWith(filesDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Skipped package ZIP entry '{Entry}': target escapes the package files directory.", entry.FullName);
            return null;
        }

        return destPath;
    }

    /// <summary>Manifest fields the orchestrator reads; the registry validates the rest.</summary>
    private sealed class PackageManifest
    {
        public List<PackageFileDeclaration> PackageFiles { get; init; } = [];
    }

    private sealed class PackageFileDeclaration
    {
        public string Name { get; init; } = string.Empty;
    }

    public Task DeleteVolumes(string projectName)
    {
        var volumesPath = $"{GetInternalWorkspacePath(projectName)}/volumes";
        if (!Directory.Exists(volumesPath)) return Task.CompletedTask;
        DeleteDirectory(volumesPath);
        logger.LogInformation("Deleted volume '{Volume}' for project '{Project}'", volumesPath, projectName);
        return Task.CompletedTask;
    }
}
