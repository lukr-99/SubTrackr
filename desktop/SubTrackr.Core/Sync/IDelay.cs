namespace SubTrackr.Core.Sync;

/// <summary>Waits between retries; tests replace it so they never sleep.</summary>
public interface IDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}
