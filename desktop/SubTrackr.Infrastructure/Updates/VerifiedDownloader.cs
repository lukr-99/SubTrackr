using System.Security.Cryptography;
using SubTrackr.Core.Updates;

namespace SubTrackr.Infrastructure.Updates;

/// <summary>
/// Downloads an update into a fresh folder under <paramref name="root"/> (the user's own temp
/// folder in the app) and checks it against the published SHA-256 while it streams. A mismatch,
/// a bad checksum file, or an oversized download deletes the file and throws.
/// </summary>
public sealed class VerifiedDownloader(HttpClient http, string root) : IUpdateDownloader
{
    private const long MaxInstallerBytes = 512L * 1024 * 1024;
    private const int MaxChecksumBytes = 4096;

    public async Task<string> DownloadAsync(UpdateOffer offer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);
        if (offer.AssetUrl.Scheme != Uri.UriSchemeHttps || offer.ChecksumUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new UpdateVerificationException("The update is not served over HTTPS.");
        }

        if (!string.Equals(Path.GetFileName(offer.AssetName), offer.AssetName, StringComparison.Ordinal))
        {
            throw new UpdateVerificationException("The installer name is not a plain file name.");
        }

        var expected = ChecksumFile.Parse(await ReadChecksumAsync(offer.ChecksumUrl, cancellationToken).ConfigureAwait(false))
            ?? throw new UpdateVerificationException("The published checksum is not a SHA-256.");

        RemoveOldDownloads();
        var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, offer.AssetName);
        try
        {
            var actual = await DownloadAndHashAsync(offer.AssetUrl, path, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new UpdateVerificationException("The download does not match its published checksum.");
            }

            return path;
        }
        catch
        {
            Directory.Delete(folder, recursive: true);
            throw;
        }
    }

    private async Task<string> ReadChecksumAsync(Uri url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var buffer = new byte[MaxChecksumBytes + 1];
            var total = 0;
            int read;
            while (total < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
            }

            if (total > MaxChecksumBytes)
            {
                throw new UpdateVerificationException("The published checksum file is too large.");
            }

            return System.Text.Encoding.UTF8.GetString(buffer, 0, total);
        }
    }

    private async Task<string> DownloadAndHashAsync(Uri url, string path, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await using (target.ConfigureAwait(false))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > MaxInstallerBytes)
                    {
                        throw new UpdateVerificationException("The download is larger than any SubTrackr installer.");
                    }

                    hash.AppendData(buffer, 0, read);
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    // Earlier downloads are no longer needed once a new one starts; failures to clean up are harmless.
    private void RemoveOldDownloads()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // An installer from an earlier update may still be running from there.
            }
        }
    }
}
