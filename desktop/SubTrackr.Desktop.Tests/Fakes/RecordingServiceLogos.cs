using System.Windows.Media;
using System.Windows.Media.Imaging;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Records every service logo requested and answers with a blank in-memory image, off the network.</summary>
public sealed class RecordingServiceLogos : IServiceLogoSource
{
    public List<string> Requested { get; } = [];

    public ImageSource? Load(string website)
    {
        Requested.Add(website);
        var image = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
        image.Freeze();
        return image;
    }
}
