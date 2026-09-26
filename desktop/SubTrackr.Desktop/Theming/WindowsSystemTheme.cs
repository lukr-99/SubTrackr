using Microsoft.Win32;

namespace SubTrackr.Desktop.Theming;

/// <summary>
/// Windows' "app mode": <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize</c>,
/// value <c>AppsUseLightTheme</c> (0 means dark; missing means light). Windows announces a change
/// through <see cref="SystemEvents.UserPreferenceChanged"/> with the General category.
/// </summary>
public sealed class WindowsSystemTheme : ISystemTheme, IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public WindowsSystemTheme()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event EventHandler? Changed;

    public bool AppsUseDarkTheme
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
