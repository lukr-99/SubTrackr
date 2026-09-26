using System.Globalization;
using System.Text.RegularExpressions;

namespace SubTrackr.Core.Updates;

/// <summary>
/// A SemVer version such as <c>0.3.0</c> or <c>0.3.0-dev</c>. Releases compare by major, minor,
/// and patch as numbers (so 0.10.0 is newer than 0.9.0); a pre-release sorts before its release.
/// </summary>
public sealed partial record ReleaseVersion(int Major, int Minor, int Patch, string PreRelease) : IComparable<ReleaseVersion>
{
    public bool IsPreRelease => PreRelease.Length > 0;

    /// <summary>A running version: <c>X.Y.Z</c> with an optional <c>-suffix</c>; null otherwise.</summary>
    public static ReleaseVersion? TryParse(string? text) => Parse(VersionPattern().Match(text?.Trim() ?? ""));

    /// <summary>A release tag, which must be exactly <c>vX.Y.Z</c> (SPEC.md section 11); null otherwise.</summary>
    public static ReleaseVersion? TryParseTag(string? tag) => Parse(TagPattern().Match(tag ?? ""));

    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var core = (Major, Minor, Patch).CompareTo((other.Major, other.Minor, other.Patch));
        if (core != 0)
        {
            return core;
        }

        return (IsPreRelease, other.IsPreRelease) switch
        {
            (false, false) => 0,
            (true, false) => -1,
            (false, true) => 1,
            _ => string.CompareOrdinal(PreRelease, other.PreRelease),
        };
    }

    public static bool operator <(ReleaseVersion left, ReleaseVersion right) => Compare(left, right) < 0;

    public static bool operator >(ReleaseVersion left, ReleaseVersion right) => Compare(left, right) > 0;

    public static bool operator <=(ReleaseVersion left, ReleaseVersion right) => Compare(left, right) <= 0;

    public static bool operator >=(ReleaseVersion left, ReleaseVersion right) => Compare(left, right) >= 0;

    public override string ToString() => IsPreRelease ? $"{Major}.{Minor}.{Patch}-{PreRelease}" : $"{Major}.{Minor}.{Patch}";

    private static int Compare(ReleaseVersion? left, ReleaseVersion? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

    private static ReleaseVersion? Parse(Match match)
    {
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            && int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            && int.TryParse(match.Groups["patch"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var patch)
                ? new ReleaseVersion(major, minor, patch, match.Groups["pre"].Value)
                : null;
    }

    [GeneratedRegex(@"^(?<major>\d{1,9})\.(?<minor>\d{1,9})\.(?<patch>\d{1,9})(?:-(?<pre>[0-9A-Za-z.-]+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();

    [GeneratedRegex(@"^v(?<major>\d{1,9})\.(?<minor>\d{1,9})\.(?<patch>\d{1,9})$", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();
}
