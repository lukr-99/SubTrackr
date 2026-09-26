using System.Reflection;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// What this build is: its version as Directory.Build.props stamps it (<c>0.3.0</c> for a release,
/// <c>0.3.0-dev</c> for every other build).
/// </summary>
public sealed record BuildInfo(string Version)
{
    /// <summary>The X.Y.Z part, without any pre-release suffix.</summary>
    public string CoreVersion => Version.Split('-', '+')[0];

    public static BuildInfo FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return new BuildInfo(string.IsNullOrWhiteSpace(informational) ? "0.0.0-dev" : informational.Trim());
    }
}
