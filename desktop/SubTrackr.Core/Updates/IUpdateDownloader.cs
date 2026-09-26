namespace SubTrackr.Core.Updates;

/// <summary>Downloads an offered installer into app-private temporary storage and verifies it.</summary>
public interface IUpdateDownloader
{
    /// <summary>
    /// The path of the verified installer. Throws <see cref="UpdateVerificationException"/> when the
    /// checksum file is unusable or the download does not match it; the file is then already gone.
    /// </summary>
    Task<string> DownloadAsync(UpdateOffer offer, CancellationToken cancellationToken);
}
