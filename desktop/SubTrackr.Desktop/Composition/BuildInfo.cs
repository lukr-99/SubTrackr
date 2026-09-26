using System.Reflection;
using SubTrackr.Core.Updates;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// What this build is: its version as Directory.Build.props stamps it, <c>0.3.0</c> for a release
/// and <c>0.3.0-dev</c> for every other build (SPEC.md section 7).
/// </summary>
public sealed record BuildInfo(string Version)
{
    /// <summary>A development build: anything that is not a plain X.Y.Z release version.</summary>
    public bool IsDevBuild => ReleaseVersion.TryParse(Version) is not { IsPreRelease: false };

    public static BuildInfo FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return new BuildInfo(string.IsNullOrWhiteSpace(informational) ? "0.0.0-dev" : informational.Trim());
    }
}
