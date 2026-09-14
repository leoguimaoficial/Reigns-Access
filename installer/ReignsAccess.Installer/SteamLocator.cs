using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ReignsAccess.Installer;

internal static partial class SteamLocator
{
    private const string ReignsAppId = "474750";

    public static string? FindReignsDirectory()
    {
        foreach (var library in FindSteamLibraries())
        {
            var steamApps = Path.Combine(library, "steamapps");
            var manifest = Path.Combine(steamApps, $"appmanifest_{ReignsAppId}.acf");
            var installDirectory = "Reigns";

            if (File.Exists(manifest))
            {
                try
                {
                    var match = InstallDirRegex().Match(File.ReadAllText(manifest));
                    if (match.Success)
                        installDirectory = UnescapeVdf(match.Groups["value"].Value);
                }
                catch
                {
                    // Keep trying the conventional directory name.
                }
            }

            var candidate = Path.Combine(steamApps, "common", installDirectory);
            if (ValidateGameDirectory(candidate) is null)
                return Path.GetFullPath(candidate);
        }

        return null;
    }

    public static string? ValidateGameDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Select the Reigns game folder.";

        try
        {
            var fullPath = Path.GetFullPath(path.Trim().Trim('"'));
            if (!File.Exists(Path.Combine(fullPath, "Reigns.exe")))
                return "Reigns.exe was not found in the selected folder.";
            if (!Directory.Exists(Path.Combine(fullPath, "Reigns_Data")))
                return "The Reigns_Data folder was not found in the selected folder.";

            var commonDirectory = Directory.GetParent(fullPath);
            var steamAppsDirectory = commonDirectory?.Parent;
            if (commonDirectory is null || steamAppsDirectory is null ||
                !commonDirectory.Name.Equals("common", StringComparison.OrdinalIgnoreCase) ||
                !steamAppsDirectory.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(Path.Combine(steamAppsDirectory.FullName, $"appmanifest_{ReignsAppId}.acf")))
            {
                return "This does not appear to be the Steam installation of Reigns. The GOG version is not currently supported.";
            }
            return null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return "The selected game folder is not a valid Windows path.";
        }
    }

    internal static IReadOnlyList<string> ParseLibraryFolders(string contents)
    {
        return LibraryPathRegex()
            .Matches(contents)
            .Select(match => UnescapeVdf(match.Groups["value"].Value))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static IEnumerable<string> FindSteamLibraries()
    {
        var roots = new List<string?>
        {
            ReadRegistryValue(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
            ReadRegistryValue(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
        };

        var discovered = new List<string>();
        foreach (var root in roots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var normalizedRoot = Path.GetFullPath(root!.Replace('/', Path.DirectorySeparatorChar));
            discovered.Add(normalizedRoot);

            var vdfPath = Path.Combine(normalizedRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath))
                continue;

            try
            {
                discovered.AddRange(ParseLibraryFolders(File.ReadAllText(vdfPath)));
            }
            catch
            {
                // An unreadable Steam config should not prevent manual selection.
            }
        }

        return discovered
            .Where(Directory.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? ReadRegistryValue(RegistryKey root, string keyName, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(keyName);
            return key?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }

    private static string UnescapeVdf(string value) =>
        value.Replace("\\\\", "\\").Replace("\\\"", "\"");

    [GeneratedRegex("\\\"path\\\"\\s+\\\"(?<value>[^\\\"]+)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryPathRegex();

    [GeneratedRegex("\\\"installdir\\\"\\s+\\\"(?<value>[^\\\"]+)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex InstallDirRegex();
}
