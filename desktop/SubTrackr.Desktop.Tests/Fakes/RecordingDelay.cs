using SubTrackr.Core.Sync;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Remembers each wait and returns at once.</summary>
public sealed class RecordingDelay : IDelay
{
    public List<TimeSpan> Waits { get; } = [];

    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        Waits.Add(delay);
        return Task.CompletedTask;
    }
}
