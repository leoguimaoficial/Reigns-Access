using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace ReignsAccess.Installer;

internal sealed class PackageInstaller
{
    internal const string ManifestRelativePath = "BepInEx/config/ReignsAccess.installer.json";
    private static readonly Version MinimumBepInExVersion = new(5, 4, 23, 5);

    public string? GetInstalledVersion(string gameDirectory)
    {
        try
        {
            var manifest = LoadManifest(gameDirectory);
            if (manifest is not null)
                return manifest.ModVersion;

            return File.Exists(Path.Combine(gameDirectory, "BepInEx", "plugins", "ReignsAccess.dll"))
                ? "manual/unknown"
                : null;
        }
        catch
        {
            return File.Exists(Path.Combine(gameDirectory, "BepInEx", "plugins", "ReignsAccess.dll"))
                ? "manual/unknown"
                : null;
        }
    }

    public async Task InstallReleaseAsync(
        ModRelease release,
        string gameDirectory,
        GitHubReleaseClient releaseClient,
        IProgress<int>? progress,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        EnsureReady(gameDirectory);
        var workingDirectory = CreateWorkingDirectory();
        try
        {
            var archivePath = Path.Combine(workingDirectory, "package.zip");
            log($"Downloading {release.Asset.Name}...");
            var downloadProgress = new Progress<int>(value => progress?.Report(value * 60 / 100));
            await releaseClient.DownloadAsync(release.Asset, archivePath, downloadProgress, cancellationToken);
            log("Download complete. Verifying package...");

            await InstallArchiveAsync(
                archivePath,
                release.Tag,
                release.Asset.Digest,
                gameDirectory,
                workingDirectory,
                progress,
                log,
                cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    public async Task InstallLocalArchiveAsync(
        string archivePath,
        string gameDirectory,
        IProgress<int>? progress,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        EnsureReady(gameDirectory);
        var workingDirectory = CreateWorkingDirectory();
        try
        {
            await InstallArchiveAsync(
                archivePath,
                "local package",
                null,
                gameDirectory,
                workingDirectory,
                progress,
                log,
                cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    public void Uninstall(string gameDirectory, Action<string> log)
    {
        EnsureReady(gameDirectory);

        var plugin = Path.Combine(gameDirectory, "BepInEx", "plugins", "ReignsAccess.dll");
        var languages = Path.Combine(gameDirectory, "BepInEx", "plugins", "ReignsAccess_Lang");
        var manifest = Path.Combine(gameDirectory, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(plugin))
        {
            File.Delete(plugin);
            log("Removed ReignsAccess.dll.");
        }

        if (Directory.Exists(languages))
        {
            Directory.Delete(languages, recursive: true);
            log("Removed Reigns Access language files.");
        }

        if (File.Exists(manifest))
            File.Delete(manifest);

        log("BepInEx and screen-reader bridge files were kept because other mods may use them.");
    }

    private static async Task InstallArchiveAsync(
        string archivePath,
        string version,
        string? expectedDigest,
        string gameDirectory,
        string workingDirectory,
        IProgress<int>? progress,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("The selected package does not exist.", archivePath);

        await VerifyDigestAsync(archivePath, expectedDigest, cancellationToken);

        var stagingDirectory = Path.Combine(workingDirectory, "staging");
        Directory.CreateDirectory(stagingDirectory);
        var files = ExtractSafely(archivePath, stagingDirectory, cancellationToken);
        ValidatePayload(stagingDirectory);
        log($"Package verified ({files.Count} files). Installing...");

        var backupDirectory = Path.Combine(workingDirectory, "backup");
        var copied = new List<(string Destination, bool Existed)>();
        try
        {
            for (var index = 0; index < files.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = files[index];
                var source = Path.Combine(stagingDirectory, relativePath);
                var destination = GetPathInside(gameDirectory, relativePath);
                var existed = File.Exists(destination);

                if (existed)
                {
                    var backup = Path.Combine(backupDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Copy(destination, backup, overwrite: true);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
                copied.Add((destination, existed));
                progress?.Report(60 + (index + 1) * 39 / files.Count);
            }

            SaveManifest(gameDirectory, new InstallManifest
            {
                ModVersion = version,
                InstalledAtUtc = DateTimeOffset.UtcNow,
                Files = files.Select(path => path.Replace('\\', '/')).ToList()
            });

            progress?.Report(100);
            log($"Reigns Access {version} was installed successfully.");
        }
        catch
        {
            log("Installation failed. Restoring the previous files...");
            RollBack(copied, backupDirectory, gameDirectory);
            throw;
        }
    }

    internal static IReadOnlyList<string> ExtractSafely(
        string archivePath,
        string destination,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var fileEntries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToArray();
        if (fileEntries.Length == 0)
            throw new InvalidDataException("The package is empty.");

        var normalizedPaths = fileEntries.Select(entry => NormalizeArchivePath(entry.FullName)).ToArray();
        var commonRoot = FindCommonRoot(normalizedPaths);
        var extracted = new List<string>(fileEntries.Length);

        for (var index = 0; index < fileEntries.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = normalizedPaths[index];
            if (commonRoot is not null)
                relativePath = relativePath[(commonRoot.Length + 1)..];
            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var outputPath = GetPathInside(destination, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using var input = fileEntries[index].Open();
            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
            extracted.Add(relativePath);
        }

        return extracted;
    }

    internal static string NormalizeArchivePath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (Path.IsPathRooted(path) || parts.Length == 0 ||
            parts.Any(part => part is "." or ".." || part.Contains(':')))
        {
            throw new InvalidDataException($"Unsafe path in package: {path}");
        }

        return string.Join('/', parts);
    }

    internal static string? FindCommonRoot(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0 || paths.Any(path => !path.Contains('/')))
            return null;

        var first = paths[0][..paths[0].IndexOf('/')];
        return paths.All(path => path.StartsWith(first + "/", StringComparison.OrdinalIgnoreCase))
            ? first
            : null;
    }

    private static void ValidatePayload(string stagingDirectory)
    {
        var requiredFiles = new[]
        {
            "BepInEx/plugins/ReignsAccess.dll",
            "BepInEx/core/BepInEx.dll",
            "doorstop_config.ini",
            "winhttp.dll",
            "Tolk.dll",
            "nvdaControllerClient64.dll"
        };

        var missing = requiredFiles
            .Where(path => !File.Exists(Path.Combine(stagingDirectory, path.Replace('/', Path.DirectorySeparatorChar))))
            .ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException("The release is not a complete installer package. Missing: " + string.Join(", ", missing));

        var bepinExPath = Path.Combine(stagingDirectory, "BepInEx", "core", "BepInEx.dll");
        var versionText = FileVersionInfo.GetVersionInfo(bepinExPath).FileVersion;
        if (!Version.TryParse(versionText?.Split('+')[0], out var version) || version < MinimumBepInExVersion)
        {
            throw new InvalidDataException(
                $"This package contains BepInEx {versionText ?? "of unknown version"}. " +
                $"Reigns now requires BepInEx {MinimumBepInExVersion} or newer in the 5.x line.");
        }
    }

    private static async Task VerifyDigestAsync(
        string archivePath,
        string? expectedDigest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expectedDigest) ||
            !expectedDigest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            return;

        await using var stream = File.OpenRead(archivePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        var actual = Convert.ToHexString(hash);
        var expected = expectedDigest["sha256:".Length..];
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded package failed its SHA-256 integrity check.");
    }

    private static void EnsureReady(string gameDirectory)
    {
        var validationError = SteamLocator.ValidateGameDirectory(gameDirectory);
        if (validationError is not null)
            throw new InvalidOperationException(validationError);

        if (Process.GetProcessesByName("Reigns").Any())
            throw new InvalidOperationException("Close Reigns before installing, updating, or uninstalling the mod.");
    }

    private static string CreateWorkingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ReignsAccessInstaller", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetPathInside(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"A package path escapes the game folder: {relativePath}");
        return fullPath;
    }

    private static InstallManifest? LoadManifest(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return null;
        return JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(path));
    }

    private static void SaveManifest(string gameDirectory, InstallManifest manifest)
    {
        var path = Path.Combine(gameDirectory, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, overwrite: true);
    }

    private static void RollBack(
        IEnumerable<(string Destination, bool Existed)> copied,
        string backupDirectory,
        string gameDirectory)
    {
        foreach (var item in copied.Reverse())
        {
            try
            {
                if (item.Existed)
                {
                    var relative = Path.GetRelativePath(gameDirectory, item.Destination);
                    File.Copy(Path.Combine(backupDirectory, relative), item.Destination, overwrite: true);
                }
                else if (File.Exists(item.Destination))
                {
                    File.Delete(item.Destination);
                }
            }
            catch
            {
                // Continue restoring every other file, then surface the original error.
            }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Temporary files can be cleaned up by Windows later.
        }
    }

    private sealed class InstallManifest
    {
        public int SchemaVersion { get; init; } = 1;
        public string ModVersion { get; init; } = "";
        public DateTimeOffset InstalledAtUtc { get; init; }
        public List<string> Files { get; init; } = [];
    }
}
