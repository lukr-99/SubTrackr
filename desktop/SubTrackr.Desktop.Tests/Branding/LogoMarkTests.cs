using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.Tests.Theming;

namespace SubTrackr.Desktop.Tests.Branding;

/// <summary>
/// The side-bar mark must be contracts/design/logo.json in the tokens.json brand colors (SPEC.md
/// section 10). The expected shapes are computed here from the contract files themselves.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class LogoMarkTests
{
    private static readonly string LogoPath = Path.Combine(AppContext.BaseDirectory, "design", "logo.json");
    private static readonly string TokensPath = Path.Combine(AppContext.BaseDirectory, "design", "tokens.json");

    [Fact]
    public Task Drawing_IsTheContractGeometryAndColors() => WpfHost.RunAsync(() =>
    {
        using var logo = JsonDocument.Parse(File.ReadAllText(LogoPath));
        using var tokens = JsonDocument.Parse(File.ReadAllText(TokensPath));
        var root = logo.RootElement;
        var brand = tokens.RootElement.GetProperty("brand");
        var canvas = root.GetProperty("canvas").GetDouble();
        var drawing = new LogoMark().CreateDrawing();

        var heights = root.GetProperty("barHeights").EnumerateArray().Select(h => h.GetDouble()).ToList();
        Assert.Equal(1 + heights.Count, drawing.Children.Count);

        var square = (GeometryDrawing)drawing.Children[0];
        var squareShape = (RectangleGeometry)square.Geometry;
        Assert.Equal(new Rect(0, 0, canvas, canvas), squareShape.Rect);
        Assert.Equal(root.GetProperty("cornerRadius").GetDouble(), squareShape.RadiusX);
        Assert.Equal(root.GetProperty("cornerRadius").GetDouble(), squareShape.RadiusY);
        var gradient = (LinearGradientBrush)square.Brush;
        Assert.Equal(new Point(0, 0), gradient.StartPoint);
        Assert.Equal(new Point(1, 1), gradient.EndPoint);
        Assert.Equal(brand.GetProperty("gradientStart").GetString(), DesignTokensTests.Hex(gradient.GradientStops[0].Color));
        Assert.Equal(brand.GetProperty("gradientEnd").GetString(), DesignTokensTests.Hex(gradient.GradientStops[1].Color));

        var width = root.GetProperty("barWidth").GetDouble();
        var gap = root.GetProperty("barGap").GetDouble();
        var baseline = root.GetProperty("baseline").GetDouble();
        var left = (canvas - ((heights.Count * width) + ((heights.Count - 1) * gap))) / 2;
        for (var i = 0; i < heights.Count; i++)
        {
            var bar = (GeometryDrawing)drawing.Children[1 + i];
            var shape = (RectangleGeometry)bar.Geometry;
            Assert.Equal(new Rect(left + (i * (width + gap)), baseline - heights[i], width, heights[i]), shape.Rect);
            Assert.Equal(root.GetProperty("barCornerRadius").GetDouble(), shape.RadiusX);
            Assert.Equal(brand.GetProperty("mark").GetString(), DesignTokensTests.Hex(((SolidColorBrush)bar.Brush).Color));
        }
    });

    [Fact]
    public Task Render_PaintsTheBarsOnTheGradientInsideRoundedCorners() => WpfHost.RunAsync(() =>
    {
        var mark = new LogoMark { Width = 256, Height = 256 };
        mark.Measure(new Size(256, 256));
        mark.Arrange(new Rect(0, 0, 256, 256));
        var bitmap = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(mark);
        Screenshots.Save(bitmap, "logo-mark");

        var tallest = mark.Spec.Bar(mark.Spec.BarHeights.Count - 1);
        Assert.Equal(mark.Brand.Mark, Pixel(bitmap, tallest.X + (tallest.Width / 2), tallest.Y + (tallest.Height / 2)));
        Assert.Equal(0, Pixel(bitmap, 1, 1).A);
        var gap = mark.Spec.Bar(0).Right + (mark.Spec.BarGap / 2);
        Assert.NotEqual(mark.Brand.Mark, Pixel(bitmap, gap, mark.Spec.Baseline - 10));
    });

    private static Color Pixel(BitmapSource bitmap, double x, double y)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect((int)x, (int)y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }
}
