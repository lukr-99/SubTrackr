namespace SubTrackr.Core.Sync;

/// <summary>Waits on the given clock.</summary>
public sealed class ClockDelay(TimeProvider time) : IDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, time, cancellationToken);
}
