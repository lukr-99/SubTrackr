using System.Windows;
using System.Windows.Media;
using SubTrackr.Desktop.Theming;

namespace SubTrackr.Desktop.Branding;

/// <summary>
/// Draws the mark in <see cref="LogoSpec"/> canvas units: the gradient rounded square first, then
/// one rectangle per bar, each grown from its baseline by its progress (1 is full height).
/// </summary>
public static class LogoDrawing
{
    public static DrawingGroup Create(LogoSpec spec, BrandPalette brand, IReadOnlyList<double> barProgress)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(brand);
        ArgumentNullException.ThrowIfNull(barProgress);

        var group = new DrawingGroup();
        var background = new LinearGradientBrush(brand.GradientStart, brand.GradientEnd, new Point(0, 0), new Point(1, 1));
        group.Children.Add(new GeometryDrawing(
            background,
            null,
            new RectangleGeometry(new Rect(0, 0, spec.Canvas, spec.Canvas), spec.CornerRadius, spec.CornerRadius)));

        var mark = new SolidColorBrush(brand.Mark);
        for (var i = 0; i < spec.BarHeights.Count; i++)
        {
            var full = spec.Bar(i);
            var height = full.Height * Math.Clamp(barProgress[i], 0, 1);
            var bar = new Rect(full.X, full.Bottom - height, full.Width, height);
            group.Children.Add(new GeometryDrawing(
                mark,
                null,
                new RectangleGeometry(bar, spec.BarCornerRadius, spec.BarCornerRadius)));
        }

        group.Freeze();
        return group;
    }
}
