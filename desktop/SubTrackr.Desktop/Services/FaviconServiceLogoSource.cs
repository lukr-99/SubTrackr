using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SubTrackr.Desktop.Services;

/// <summary>
/// Service logos from Google's favicon service (SPEC.md section 12), which learns each domain. WPF
/// downloads the image in the background.
/// </summary>
public sealed class FaviconServiceLogoSource : IServiceLogoSource
{
    /// <summary>The favicon address for <paramref name="website"/>, or null when it makes no valid URI.</summary>
    public static Uri? AddressFor(string website) =>
        Uri.TryCreate($"https://www.google.com/s2/favicons?domain={website.Trim()}&sz=64", UriKind.Absolute, out var address)
            ? address
            : null;

    public ImageSource? Load(string website) =>
        AddressFor(website) is { } address
            ? BitmapFrame.Create(address, BitmapCreateOptions.None, BitmapCacheOption.Default)
            : null;
}
