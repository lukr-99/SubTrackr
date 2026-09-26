using SubTrackr.Core.Diagnostics;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>Keeps every logged line for assertions.</summary>
public sealed class RecordingLog : IAppLog
{
    private readonly List<string> lines = [];

    public IReadOnlyList<string> Lines => lines;

    public void Info(string message) => lines.Add("INFO " + message);

    public void Error(string message, Exception? exception = null) =>
        lines.Add("ERROR " + message + (exception is null ? "" : " | " + exception.Message));
}
