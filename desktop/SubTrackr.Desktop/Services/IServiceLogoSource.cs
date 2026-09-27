using System.Windows.Media;

namespace SubTrackr.Desktop.Services;

/// <summary>
/// Where service logos come from (SPEC.md section 12). Asking for one starts the request, so callers
/// ask only when the logo will show and service logos are on.
/// </summary>
public interface IServiceLogoSource
{
    /// <summary>
    /// The service logo for <paramref name="website"/>, possibly still loading; null when the website
    /// makes no valid address.
    /// </summary>
    ImageSource? Load(string website);
}
