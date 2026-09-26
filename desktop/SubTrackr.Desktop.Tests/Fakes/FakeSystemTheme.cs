using SubTrackr.Desktop.Theming;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>A Windows app mode the test sets; <see cref="Switch"/> raises the change like Windows does.</summary>
public sealed class FakeSystemTheme : ISystemTheme
{
    public event EventHandler? Changed;

    public bool AppsUseDarkTheme { get; private set; }

    public void Switch(bool dark)
    {
        AppsUseDarkTheme = dark;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
