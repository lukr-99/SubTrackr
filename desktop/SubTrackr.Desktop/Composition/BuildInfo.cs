using System.Reflection;
using SubTrackr.Core.Updates;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// What this build is: its version as Directory.Build.props stamps it, <c>0.3.0</c> for a release
/// and <c>0.3.0-dev</c> for every other build (SPEC.md section 7). A dev build keeps its own name,
/// data folder, and single-instance mutex, so it runs beside the installed app.
/// </summary>
public sealed record BuildInfo(string Version)
{
    /// <summary>A development build: anything that is not a plain X.Y.Z release version.</summary>
    public bool IsDevBuild => ReleaseVersion.TryParse(Version) is not { IsPreRelease: false };

    public string ProductName => IsDevBuild ? "SubTrackr Dev" : "SubTrackr";

    /// <summary>The release name is also the installer's AppMutex, so the installer waits for the app.</summary>
    public string InstanceMutexName => IsDevBuild ? "SubTrackr_Dev_SingleInstance_7f3a" : "SubTrackr_SingleInstance_7f3a";

    public static BuildInfo FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return new BuildInfo(string.IsNullOrWhiteSpace(informational) ? "0.0.0-dev" : informational.Trim());
    }
}
