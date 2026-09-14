namespace ReignsAccess.Installer;

internal enum VersionRelation
{
    Unknown,
    Same,
    Newer,
    Older
}

internal static class ModVersionComparer
{
    /// <summary>
    /// Compares a release selected in the installer with the installed mod version.
    /// The installer itself deliberately has no user-facing release version.
    /// </summary>
    public static VersionRelation Compare(string installed, string selected)
    {
        if (installed.Equals(selected, StringComparison.OrdinalIgnoreCase))
            return VersionRelation.Same;

        if (!TryCompare(selected, installed, out var comparison))
        {
            return VersionRelation.Unknown;
        }

        if (comparison > 0)
            return VersionRelation.Newer;
        if (comparison < 0)
            return VersionRelation.Older;

        return VersionRelation.Same;
    }

    public static int CompareReleaseTags(string left, string right)
    {
        return TryCompare(left, right, out var comparison)
            ? comparison
            : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCompare(string left, string right, out int comparison)
    {
        comparison = 0;
        if (!TryParse(left, out var leftVersion) || !TryParse(right, out var rightVersion))
            return false;

        comparison = leftVersion.Numeric.CompareTo(rightVersion.Numeric);
        if (comparison != 0)
            return true;

        if (leftVersion.Prerelease is null && rightVersion.Prerelease is not null)
        {
            comparison = 1;
            return true;
        }
        if (leftVersion.Prerelease is not null && rightVersion.Prerelease is null)
        {
            comparison = -1;
            return true;
        }

        comparison = string.Compare(
            leftVersion.Prerelease,
            rightVersion.Prerelease,
            StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static bool TryParse(string value, out ParsedVersion parsed)
    {
        var normalized = value.Trim().TrimStart('v', 'V');
        var pieces = normalized.Split('-', 2, StringSplitOptions.TrimEntries);
        if (Version.TryParse(pieces[0], out var numeric))
        {
            parsed = new ParsedVersion(numeric, pieces.Length == 2 ? pieces[1] : null);
            return true;
        }

        parsed = default;
        return false;
    }

    private readonly record struct ParsedVersion(Version Numeric, string? Prerelease);
}
