using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SubTrackr.Desktop.Services;

/// <summary>File Explorer and the WPF application lifetime.</summary>
public sealed class WpfDesktopServices : IDesktopServices
{
    public void OpenFolder(string path)
    {
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
    }

    public void Shutdown() => Application.Current.Shutdown();
}
