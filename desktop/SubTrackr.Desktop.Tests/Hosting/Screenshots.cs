using System.Windows.Media.Imaging;

namespace SubTrackr.Desktop.Tests.Hosting;

/// <summary>
/// Saves a rendered bitmap as a PNG in the folder named by SUBTRACKR_SCREENSHOTS, so a person can
/// look at what the off-screen tests drew. Without the variable it only checks the bitmap is not
/// empty.
/// </summary>
public static class Screenshots
{
    public static void Save(BitmapSource bitmap, string name)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        Assert.True(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0);
        var folder = Environment.GetEnvironmentVariable("SUBTRACKR_SCREENSHOTS");
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(folder, name + ".png"));
        encoder.Save(output);
    }
}
