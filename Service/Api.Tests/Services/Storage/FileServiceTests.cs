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
using AppOrchestrator.Api.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppOrchestrator.Api.Tests.Services.Storage;

public class FileServiceTests
{
    [Fact]
    public async Task WriteEnvFileAsync_AndReadEnvFileAsync_RoundtripSpecialValues()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            Directory.CreateDirectory(workspace);

            var source = new Dictionary<string, string>
            {
                ["PLAIN"] = "value",
                ["WITH_SPACES"] = "hello world",
                ["WITH_EQUALS"] = "a=b",
                ["WITH_QUOTES"] = "\"quoted\""
            };

            await sut.WriteEnvFileAsync(workspace, source, CancellationToken.None);
            var read = await sut.ReadEnvFileAsync(workspace, CancellationToken.None);

            Assert.Equal(source["PLAIN"], read["PLAIN"]);
            Assert.Equal(source["WITH_SPACES"], read["WITH_SPACES"]);
            Assert.Equal(source["WITH_EQUALS"], read["WITH_EQUALS"]);
            Assert.Contains("quoted", read["WITH_QUOTES"]);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public void CopyDirectory_CopiesPackageFilesAndVolumeData()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var source = Path.Combine(root, "src");
            var destination = Path.Combine(root, "dst");
            Directory.CreateDirectory(Path.Combine(source, "package_files"));
            Directory.CreateDirectory(Path.Combine(source, "volumes", "postgres"));

            File.WriteAllText(Path.Combine(source, "docker-compose.yml"), "services: {}");
            File.WriteAllText(Path.Combine(source, ".env"), "A=B");
            File.WriteAllText(Path.Combine(source, "package_files", "gaeco-realm.json"), "{}");
            File.WriteAllText(Path.Combine(source, "volumes", "postgres", "data"), "rows");

            sut.CopyDirectory(source, destination, overwrite: true);

            Assert.True(File.Exists(Path.Combine(destination, "docker-compose.yml")));
            Assert.True(File.Exists(Path.Combine(destination, ".env")));
            Assert.True(File.Exists(Path.Combine(destination, "package_files", "gaeco-realm.json")));
            Assert.True(File.Exists(Path.Combine(destination, "volumes", "postgres", "data")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_WithNullStream_LeavesExistingFilesUntouched()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            var packageFiles = Path.Combine(workspace, "package_files");
            Directory.CreateDirectory(packageFiles);
            File.WriteAllText(Path.Combine(packageFiles, "keep.json"), "{}");

            await sut.ExtractPackageFilesAsync(workspace, null, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(packageFiles, "keep.json")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_WithEmptyStream_DoesNotThrow()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            Directory.CreateDirectory(workspace);

            using var empty = new MemoryStream();
            var ex = await Record.ExceptionAsync(() =>
                sut.ExtractPackageFilesAsync(workspace, empty, CancellationToken.None));

            Assert.Null(ex);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_ReplacesPreviousContent()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            var packageFiles = Path.Combine(workspace, "package_files");
            Directory.CreateDirectory(packageFiles);
            File.WriteAllText(Path.Combine(packageFiles, "dropped-by-new-version.json"), "old");

            using var zip = CreateZip(
                ("manifest.yaml", ManifestDeclaring("gaeco-realm.json")),
                ("gaeco-realm.json", "{\"realm\":\"gaeco\"}"));
            await sut.ExtractPackageFilesAsync(workspace, zip, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(packageFiles, "gaeco-realm.json")));
            Assert.False(File.Exists(Path.Combine(packageFiles, "dropped-by-new-version.json")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_ExtractsOnlyDeclaredPackageFiles()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            Directory.CreateDirectory(workspace);

            using var zip = CreateZip(
                ("manifest.yaml", ManifestDeclaring("gaeco-realm.json")),
                ("gaeco-realm.json", "{}"),
                ("undeclared.json", "ignored"));
            await sut.ExtractPackageFilesAsync(workspace, zip, CancellationToken.None);

            var packageFiles = Path.Combine(workspace, "package_files");
            Assert.True(File.Exists(Path.Combine(packageFiles, "gaeco-realm.json")));
            Assert.False(File.Exists(Path.Combine(packageFiles, "undeclared.json")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_KeepsManifestEnvSchemaAndIconInTheWorkspace()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            Directory.CreateDirectory(workspace);

            using var zip = CreateZip(
                ("manifest.yaml", ManifestDeclaring("gaeco-realm.json")),
                (".env.schema.yaml", "envSchema: []"),
                ("icon.png", "binary"),
                ("readme.md", "docs"),
                ("docker-compose.yaml", "services: {}"),
                ("gaeco-realm.json", "{}"));
            await sut.ExtractPackageFilesAsync(workspace, zip, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(workspace, "manifest.yaml")));
            Assert.True(File.Exists(Path.Combine(workspace, ".env.schema.yaml")));
            Assert.True(File.Exists(Path.Combine(workspace, "icon.png")));

            // Not needed at deploy time, so they are not written anywhere.
            Assert.False(File.Exists(Path.Combine(workspace, "readme.md")));
            Assert.False(File.Exists(Path.Combine(workspace, "package_files", "readme.md")));
            Assert.False(File.Exists(Path.Combine(workspace, "package_files", "docker-compose.yaml")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_WithoutManifest_TreatsNonRegistryFilesAsPackageFiles()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            Directory.CreateDirectory(workspace);

            // Versions published before packageFiles existed ship no manifest.
            using var zip = CreateZip(
                ("docker-compose.yaml", "services: {}"),
                (".env.schema.yaml", "envSchema: []"),
                ("icon.png", "binary"),
                ("gaeco-realm.json", "{}"));
            await sut.ExtractPackageFilesAsync(workspace, zip, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(workspace, "package_files", "gaeco-realm.json")));
            Assert.False(File.Exists(Path.Combine(workspace, "package_files", "docker-compose.yaml")));
            Assert.True(File.Exists(Path.Combine(workspace, ".env.schema.yaml")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task ExtractPackageFilesAsync_SkipsEntriesEscapingTheTargetDirectory()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspace = Path.Combine(root, "stacks", "orch-demo");
            Directory.CreateDirectory(workspace);

            using var zip = CreateZip(
                ("manifest.yaml", ManifestDeclaring("../escaped.txt", "safe.txt")),
                ("../escaped.txt", "nope"),
                ("safe.txt", "ok"));
            await sut.ExtractPackageFilesAsync(workspace, zip, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(workspace, "package_files", "safe.txt")));
            Assert.False(File.Exists(Path.Combine(workspace, "escaped.txt")));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public void ListWorkspaceProjectNames_ReturnsDirectoryNamesUnderStacksRoot()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            Directory.CreateDirectory(Path.Combine(root, "stacks", "orch-a"));
            Directory.CreateDirectory(Path.Combine(root, "stacks", "orch-b"));
            File.WriteAllText(Path.Combine(root, "stacks", "note.txt"), "not a directory");

            var names = sut.ListWorkspaceProjectNames();

            Assert.Contains("orch-a", names);
            Assert.Contains("orch-b", names);
            Assert.Equal(2, names.Count);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task DeleteVolumes_WhenVolumesDirectoryExists_DeletesIt()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var volumesPath = Path.Combine(root, "stacks", "my-stack", "volumes");
            Directory.CreateDirectory(volumesPath);
            File.WriteAllText(Path.Combine(volumesPath, "data.txt"), "some data");

            await sut.DeleteVolumes("my-stack");

            Assert.False(Directory.Exists(volumesPath));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task DeleteVolumes_WhenVolumesDirectoryDoesNotExist_CompletesWithoutError()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            Directory.CreateDirectory(Path.Combine(root, "stacks", "my-stack"));

            var ex = await Record.ExceptionAsync(() => sut.DeleteVolumes("my-stack"));

            Assert.Null(ex);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task DeleteVolumes_WhenVolumesDirectoryExists_WorkspaceDirectoryIsKept()
    {
        var root = CreateTempRoot();
        try
        {
            var sut = CreateSut(root);
            var workspacePath = Path.Combine(root, "stacks", "my-stack");
            Directory.CreateDirectory(Path.Combine(workspacePath, "volumes"));

            await sut.DeleteVolumes("my-stack");

            Assert.True(Directory.Exists(workspacePath));
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public async Task GetHostWorkspacePath_WithInvalidProjectName_ThrowsArgumentException()
    {
        var sut = CreateSut(CreateTempRoot());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetHostWorkspacePath("INVALID NAME!", CancellationToken.None));
    }

    [Fact]
    public async Task GetHostWorkspacePath_ResolvesOnlyWhereContainerAndHostPathMatch()
    {
        var sut = CreateSut(CreateTempRoot());

        if (File.Exists("/etc/hostname"))
        {
            // Containerised, with a null docker client standing in for an unreachable daemon:
            // the container path is no valid bind-mount source, so this fails instead of guessing.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.GetHostWorkspacePath("orch-demo", CancellationToken.None));
        }
        else
        {
            var result = await sut.GetHostWorkspacePath("orch-demo", CancellationToken.None);
            Assert.Equal(sut.GetInternalWorkspacePath("orch-demo"), result);
        }
    }

    private static FileService CreateSut(string root)
        => new FileService(
            dockerClient: null!,
            logger: new LoggerFactory().CreateLogger<FileService>(),
            options: Options.Create(new OrchestratorOptions { RootPath = root }));

    private static string ManifestDeclaring(params string[] fileNames)
        => "packageId: demo\nversion: 1.0.0\npackageFiles:\n"
           + string.Concat(fileNames.Select(name => $"  - name: {name}\n"));

    private static MemoryStream CreateZip(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(
                   stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static string CreateTempRoot()
        => Path.Combine(Path.GetTempPath(), "app-orch-tests", Guid.NewGuid().ToString("N"));

    private static void SafeDelete(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
