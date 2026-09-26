using SubTrackr.Core.Diagnostics;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Keeps every logged line for assertions.</summary>
public sealed class RecordingLog : IAppLog
{
    public List<string> Lines { get; } = [];

    public void Info(string message) => Lines.Add("INFO " + message);

    public void Error(string message, Exception? exception = null) => Lines.Add("ERROR " + message);
}
