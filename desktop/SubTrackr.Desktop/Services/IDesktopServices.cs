namespace SubTrackr.Desktop.Services;

/// <summary>What the view models ask of the operating system and the app process.</summary>
public interface IDesktopServices
{
    /// <summary>Shows a folder in File Explorer.</summary>
    void OpenFolder(string path);

    /// <summary>Opens an https page in the default browser.</summary>
    void OpenUrl(Uri url);

    /// <summary>Ends the app (for example once an installer has started).</summary>
    void Shutdown();
}
