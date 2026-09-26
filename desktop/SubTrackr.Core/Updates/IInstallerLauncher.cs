namespace SubTrackr.Core.Updates;

/// <summary>Starts a verified installer.</summary>
public interface IInstallerLauncher
{
    /// <summary>True once the installer process is running; the app may exit only then.</summary>
    bool Launch(string installerPath);
}
