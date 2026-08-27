using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SubTrackr.Desktop.Services;

/// <summary>Turns the Windows title bar dark to match the app theme (Win10 2004+ / Win11).</summary>
public static class DarkTitleBar
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int DwmwaUseImmersiveDarkMode = 20;

    public static void Apply(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var on = 1;
            try { DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref on, sizeof(int)); }
            catch { /* older Windows: ignore */ }
        };
    }
}
