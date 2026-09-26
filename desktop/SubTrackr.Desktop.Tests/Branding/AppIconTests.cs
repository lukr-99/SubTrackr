using System.Buffers.Binary;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SubTrackr.Desktop.Shell;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.Tests.Theming;

namespace SubTrackr.Desktop.Tests.Branding;

/// <summary>
/// The .ico that Windows shows on the taskbar and title bar (rendered by
/// tools/render-desktop-icon.ps1) has a sharp PNG image at every size Windows asks for, and its
/// large image is the logo from the contract files.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class AppIconTests
{
    private static readonly int[] Sizes = [16, 20, 24, 32, 40, 48, 64, 256];

    private static readonly string IconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SubTrackr.ico");
    private static readonly string LogoPath = Path.Combine(AppContext.BaseDirectory, "design", "logo.json");
    private static readonly string TokensPath = Path.Combine(AppContext.BaseDirectory, "design", "tokens.json");

    private static readonly byte[] PngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Icon_HasAPngImageAtEverySize()
    {
        var frames = ReadFrames(File.ReadAllBytes(IconPath));

        Assert.Equal(Sizes, frames.Select(f => f.Size).Order());
        foreach (var (size, png) in frames)
        {
            Assert.True(png.AsSpan(0, 8).SequenceEqual(PngSignature), $"The {size} px image is not a PNG.");
            Assert.Equal(size, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
            Assert.Equal(size, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
        }
    }

    [Fact]
    public Task LargestImage_IsTheContractLogo() => WpfHost.RunAsync(() =>
    {
        using var logo = JsonDocument.Parse(File.ReadAllText(LogoPath));
        using var tokens = JsonDocument.Parse(File.ReadAllText(TokensPath));
        var root = logo.RootElement;
        var mark = tokens.RootElement.GetProperty("brand").GetProperty("mark").GetString();
        var png = ReadFrames(File.ReadAllBytes(IconPath)).Single(f => f.Size == 256).Png;
        var image = BitmapDecoder.Create(new MemoryStream(png), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        var bitmap = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);

        var scale = 256 / root.GetProperty("canvas").GetDouble();
        var heights = root.GetProperty("barHeights").EnumerateArray().Select(h => h.GetDouble()).ToList();
        var width = root.GetProperty("barWidth").GetDouble();
        var gap = root.GetProperty("barGap").GetDouble();
        var baseline = root.GetProperty("baseline").GetDouble();
        var left = (root.GetProperty("canvas").GetDouble() - ((heights.Count * width) + ((heights.Count - 1) * gap))) / 2;
        for (var i = 0; i < heights.Count; i++)
        {
            var centerX = left + (i * (width + gap)) + (width / 2);
            Assert.Equal(mark, DesignTokensTests.Hex(Pixel(bitmap, centerX * scale, (baseline - (heights[i] / 2)) * scale)));
            Assert.NotEqual(mark, DesignTokensTests.Hex(Pixel(bitmap, centerX * scale, (baseline - heights[i] - 4) * scale)));
        }

        Assert.Equal(0, Pixel(bitmap, 1, 1).A);
        Assert.Equal(255, Pixel(bitmap, 128, 8).A);
    });

    [Fact]
    public Task MainWindow_UsesTheIconInItsTitleBar() => WpfHost.RunAsync(() =>
    {
        using var app = TestApp.Create(resources: Application.Current.Resources);
        var window = new MainWindow(app.Graph.Main, app.Motion);

        try
        {
            var icon = Assert.IsAssignableFrom<BitmapFrame>(window.Icon);
            Assert.Equal(Sizes.Length, icon.Decoder.Frames.Count);
        }
        finally
        {
            window.Close();
        }
    });

    private static List<(int Size, byte[] Png)> ReadFrames(byte[] ico)
    {
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(ico));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2)));
        var count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4));
        var frames = new List<(int, byte[])>();
        for (var i = 0; i < count; i++)
        {
            var entry = ico.AsSpan(6 + (16 * i), 16);
            var size = entry[0] == 0 ? 256 : entry[0];
            Assert.Equal(entry[0], entry[1]);
            var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            frames.Add((size, ico.AsSpan(offset, length).ToArray()));
        }

        return frames;
    }

    private static Color Pixel(BitmapSource bitmap, double x, double y)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect((int)x, (int)y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }
}
