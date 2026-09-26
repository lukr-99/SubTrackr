using System.Net;
using System.Security.Cryptography;
using System.Text;
using SubTrackr.Core.Updates;
using SubTrackr.Infrastructure.Tests.Fakes;
using SubTrackr.Infrastructure.Updates;

namespace SubTrackr.Infrastructure.Tests.Updates;

public class VerifiedDownloaderTests
{
    private static readonly byte[] Installer = Encoding.UTF8.GetBytes("pretend installer bytes");
    private static readonly string InstallerHash = Convert.ToHexStringLower(SHA256.HashData(Installer));

    private static readonly UpdateOffer Offer = new(
        ReleaseVersion.TryParse("0.3.1")!,
        "SubTrackr-Setup-0.3.1.exe",
        new Uri("https://downloads.example/SubTrackr-Setup-0.3.1.exe"),
        "SubTrackr-Setup-0.3.1.exe.sha256",
        new Uri("https://downloads.example/SubTrackr-Setup-0.3.1.exe.sha256"));

    [Fact]
    public async Task DownloadAsync_MatchingHash_KeepsTheInstaller()
    {
        using var folder = new TemporaryDirectory();
        var downloader = new VerifiedDownloader(new HttpClient(Serve(InstallerHash.ToUpperInvariant() + "  SubTrackr-Setup-0.3.1.exe\n")), folder.Path);

        var path = await downloader.DownloadAsync(Offer, CancellationToken.None);

        Assert.Equal("SubTrackr-Setup-0.3.1.exe", Path.GetFileName(path));
        Assert.StartsWith(folder.Path, path, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Installer, File.ReadAllBytes(path));
    }

    [Fact]
    public async Task DownloadAsync_HashMismatch_DeletesTheFileAndThrows()
    {
        using var folder = new TemporaryDirectory();
        var downloader = new VerifiedDownloader(new HttpClient(Serve(new string('0', 64))), folder.Path);

        var error = await Assert.ThrowsAsync<UpdateVerificationException>(() => downloader.DownloadAsync(Offer, CancellationToken.None));

        Assert.Equal("The download does not match its published checksum.", error.Message);
        Assert.Empty(Directory.EnumerateFiles(folder.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task DownloadAsync_ChecksumFileNotAHash_DownloadsNothing()
    {
        using var folder = new TemporaryDirectory();
        var handler = Serve("not a checksum");
        var downloader = new VerifiedDownloader(new HttpClient(handler), folder.Path);

        await Assert.ThrowsAsync<UpdateVerificationException>(() => downloader.DownloadAsync(Offer, CancellationToken.None));

        Assert.Single(handler.Requests);
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder.Path));
    }

    [Fact]
    public async Task DownloadAsync_PlainHttp_IsRefused()
    {
        using var folder = new TemporaryDirectory();
        var handler = Serve(InstallerHash);
        var downloader = new VerifiedDownloader(new HttpClient(handler), folder.Path);
        var insecure = Offer with { AssetUrl = new Uri("http://downloads.example/SubTrackr-Setup-0.3.1.exe") };

        await Assert.ThrowsAsync<UpdateVerificationException>(() => downloader.DownloadAsync(insecure, CancellationToken.None));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DownloadAsync_ServerError_LeavesNothingBehind()
    {
        using var folder = new TemporaryDirectory();
        var handler = new StubHttpHandler(request => request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(InstallerHash) }
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var downloader = new VerifiedDownloader(new HttpClient(handler), folder.Path);

        await Assert.ThrowsAsync<HttpRequestException>(() => downloader.DownloadAsync(Offer, CancellationToken.None));

        Assert.Empty(Directory.EnumerateFileSystemEntries(folder.Path));
    }

    private static StubHttpHandler Serve(string checksumText) => new(request =>
        request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(checksumText) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Installer) });
}
