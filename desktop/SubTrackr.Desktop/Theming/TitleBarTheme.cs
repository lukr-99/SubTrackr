using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SubTrackr.Desktop.Theming;

/// <summary>
/// Makes a window's title bar light or dark through DWM (Windows 10 20H1 and later) to match the
/// active theme. A window reads the theme from the <c>IsDarkTheme</c> resource when its handle is
/// created; <see cref="ThemeApplier"/> updates open windows when the theme changes.
/// </summary>
public static class TitleBarTheme
{
    public const string IsDarkResourceKey = "IsDarkTheme";

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    /// <summary>Applies the current theme once the window has a handle.</summary>
    public static void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.SourceInitialized += (_, _) => Apply(window, window.TryFindResource(IsDarkResourceKey) is true);
    }

    public static void Apply(Window window, bool dark)
    {
        ArgumentNullException.ThrowIfNull(window);
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var value = dark ? 1 : 0;
        if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
        {
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref value, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
