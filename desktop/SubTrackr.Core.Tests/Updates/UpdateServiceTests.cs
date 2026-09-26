using SubTrackr.Core.Tests.Fakes;
using SubTrackr.Core.Updates;

namespace SubTrackr.Core.Tests.Updates;

public class UpdateServiceTests
{
    private readonly FakeUpdateChannel channel = new();

    [Fact]
    public async Task CheckAsync_DevBuild_IsDisabledWithoutAskingTheSource()
    {
        channel.Latest = FakeUpdateChannel.Release("0.3.1");

        var result = await Service("0.3.0-dev").CheckAsync(CancellationToken.None);

        Assert.Equal(UpdateStatus.Disabled, result.Status);
        Assert.Equal(0, channel.Checks);
    }

    [Fact]
    public async Task CheckAsync_NewerRelease_OffersTheDesktopInstaller()
    {
        channel.Latest = FakeUpdateChannel.Release("0.3.1");

        var result = await Service("0.3.0").CheckAsync(CancellationToken.None);

        Assert.Equal(UpdateStatus.Available, result.Status);
        Assert.Equal("SubTrackr-Setup-0.3.1.exe", result.Offer!.AssetName);
        Assert.Equal(new Uri("https://downloads.example/SubTrackr-Setup-0.3.1.exe.sha256"), result.Offer.ChecksumUrl);
    }

    [Fact]
    public async Task CheckAsync_NothingPublished_SaysSo()
    {
        var result = await Service("0.3.0").CheckAsync(CancellationToken.None);

        Assert.Equal(UpdateStatus.NoRelease, result.Status);
    }

    [Fact]
    public async Task CheckAsync_SourceFails_ReportsFailureAndLogs()
    {
        channel.CheckFailure = new HttpRequestException("offline");
        var log = new RecordingLog();

        var result = await Service("0.3.0", log).CheckAsync(CancellationToken.None);

        Assert.Equal(UpdateStatus.Failed, result.Status);
        Assert.Single(log.Lines);
    }

    [Fact]
    public async Task InstallAsync_Verified_LaunchesTheDownloadedFile()
    {
        var offer = (await Available()).Offer!;

        var result = await Service("0.3.0").InstallAsync(offer, CancellationToken.None);

        Assert.True(result.Started);
        Assert.Equal(["updates/SubTrackr-Setup-0.3.1.exe"], channel.Launched);
    }

    [Fact]
    public async Task InstallAsync_VerificationFails_NeverLaunches()
    {
        var offer = (await Available()).Offer!;
        channel.DownloadFailure = new UpdateVerificationException("The download does not match its published checksum.");

        var result = await Service("0.3.0").InstallAsync(offer, CancellationToken.None);

        Assert.False(result.Started);
        Assert.Equal("The download does not match its published checksum.", result.Error);
        Assert.Empty(channel.Launched);
    }

    [Fact]
    public async Task InstallAsync_InstallerDoesNotStart_ReportsIt()
    {
        var offer = (await Available()).Offer!;
        channel.InstallerStarts = false;

        var result = await Service("0.3.0").InstallAsync(offer, CancellationToken.None);

        Assert.False(result.Started);
        Assert.Equal("The installer didn't start.", result.Error);
    }

    private Task<UpdateCheckResult> Available()
    {
        channel.Latest = FakeUpdateChannel.Release("0.3.1");
        return Service("0.3.0").CheckAsync(CancellationToken.None);
    }

    private UpdateService Service(string version, RecordingLog? log = null) =>
        new(channel, channel, channel, log ?? new RecordingLog(), version);
}
