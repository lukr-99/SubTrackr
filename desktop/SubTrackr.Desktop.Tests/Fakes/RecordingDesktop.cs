using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Records what the app asked of the operating system instead of doing it.</summary>
public sealed class RecordingDesktop : IDesktopServices
{
    public List<string> OpenedFolders { get; } = [];

    public int Shutdowns { get; private set; }

    public void OpenFolder(string path) => OpenedFolders.Add(path);

    public void Shutdown() => Shutdowns++;
}
