using SubTrackr.Core.Updates;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>
/// The whole update path on fakes: the release to report (null means none published), whether the
/// download verifies, and whether the installer starts. Counts each step.
/// </summary>
public sealed class FakeUpdateChannel : IReleaseSource, IUpdateDownloader, IInstallerLauncher
{
    public PublishedRelease? Latest { get; set; }

    public bool DownloadVerifies { get; set; } = true;

    public bool InstallerStarts { get; set; } = true;

    public int Checks { get; private set; }

    public int Downloads { get; private set; }

    public List<string> Launched { get; } = [];

    public static PublishedRelease Release(string version) => new($"v{version}",
    [
        new ReleaseAsset($"SubTrackr-Setup-{version}.exe", $"https://downloads.example/SubTrackr-Setup-{version}.exe"),
        new ReleaseAsset($"SubTrackr-Setup-{version}.exe.sha256", $"https://downloads.example/SubTrackr-Setup-{version}.exe.sha256"),
    ]);

    public Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken)
    {
        Checks++;
        return Task.FromResult(Latest);
    }

    public Task<string> DownloadAsync(UpdateOffer offer, CancellationToken cancellationToken)
    {
        Downloads++;
        return DownloadVerifies
            ? Task.FromResult(Path.Combine("updates", offer.AssetName))
            : Task.FromException<string>(new UpdateVerificationException("The download does not match its published checksum."));
    }

    public bool Launch(string installerPath)
    {
        Launched.Add(installerPath);
        return InstallerStarts;
    }
}
