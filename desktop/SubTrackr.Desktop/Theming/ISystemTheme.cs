namespace SubTrackr.Desktop.Theming;

/// <summary>The operating system's light or dark app setting, which System mode follows.</summary>
public interface ISystemTheme
{
    /// <summary>Raised when the setting may have changed, on any thread.</summary>
    event EventHandler? Changed;

    bool AppsUseDarkTheme { get; }
}
