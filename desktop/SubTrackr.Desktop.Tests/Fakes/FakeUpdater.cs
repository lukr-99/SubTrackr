using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Offers <see cref="Offered"/> (null means up to date) and counts installs.</summary>
public sealed class FakeUpdater : IUpdater
{
    public string? Offered { get; set; }

    public int Installs { get; private set; }

    public Task<string?> CheckAsync(CancellationToken cancellationToken) => Task.FromResult(Offered);

    public Task InstallAsync(CancellationToken cancellationToken)
    {
        Installs++;
        return Task.CompletedTask;
    }
}
