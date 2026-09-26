using SubTrackr.Core.Updates;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>
/// Release source, downloader, and launcher in one fake: what to report, what goes wrong, and
/// what was asked of each step.
/// </summary>
public sealed class FakeUpdateChannel : IReleaseSource, IUpdateDownloader, IInstallerLauncher
{
    public PublishedRelease? Latest { get; set; }

    public Exception? CheckFailure { get; set; }

    public Exception? DownloadFailure { get; set; }

    public bool InstallerStarts { get; set; } = true;

    public int Checks { get; private set; }

    public List<string> Launched { get; } = [];

    public static PublishedRelease Release(string version) => new($"v{version}",
    [
        new ReleaseAsset($"SubTrackr-Setup-{version}.exe", $"https://downloads.example/SubTrackr-Setup-{version}.exe"),
        new ReleaseAsset($"SubTrackr-Setup-{version}.exe.sha256", $"https://downloads.example/SubTrackr-Setup-{version}.exe.sha256"),
    ]);

    public Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken)
    {
        Checks++;
        return CheckFailure is null ? Task.FromResult(Latest) : Task.FromException<PublishedRelease?>(CheckFailure);
    }

    public Task<string> DownloadAsync(UpdateOffer offer, CancellationToken cancellationToken) =>
        DownloadFailure is null
            ? Task.FromResult("updates/" + offer.AssetName)
            : Task.FromException<string>(DownloadFailure);

    public bool Launch(string installerPath)
    {
        Launched.Add(installerPath);
        return InstallerStarts;
    }
}
