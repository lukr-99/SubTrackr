using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SubTrackr.Desktop.Controls;

/// <summary>
/// A hand-rendered spending chart — no third-party charting library. Switches between
/// a donut, a bar chart, and a trend line over the same <see cref="Slices"/> data.
/// Draws directly to the <see cref="DrawingContext"/> in <see cref="OnRender"/>.
/// </summary>
public sealed class SpendChart : FrameworkElement
{
    public static readonly DependencyProperty SlicesProperty = DependencyProperty.Register(
        nameof(Slices), typeof(IEnumerable<ChartSlice>), typeof(SpendChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ChartTypeProperty = DependencyProperty.Register(
        nameof(ChartType), typeof(ChartType), typeof(SpendChart),
        new FrameworkPropertyMetadata(ChartType.Donut, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CenterTextProperty = DependencyProperty.Register(
        nameof(CenterText), typeof(string), typeof(SpendChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CenterSubtextProperty = DependencyProperty.Register(
        nameof(CenterSubtext), typeof(string), typeof(SpendChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<ChartSlice>? Slices
    {
        get => (IEnumerable<ChartSlice>?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    public ChartType ChartType
    {
        get => (ChartType)GetValue(ChartTypeProperty);
        set => SetValue(ChartTypeProperty, value);
    }

    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public string CenterSubtext
    {
        get => (string)GetValue(CenterSubtextProperty);
        set => SetValue(CenterSubtextProperty, value);
    }

    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF5));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6));
    private static readonly Brush TrackBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3B, 0x42));
    private static readonly Typeface Face = new("Segoe UI Variable");
    private static readonly Typeface FaceSemibold =
        new(new FontFamily("Segoe UI Variable"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    protected override void OnRender(DrawingContext dc)
    {
        var slices = (Slices?.Where(s => s.Value > 0).ToList()) ?? new List<ChartSlice>();
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 1 || h <= 1) return;

        if (slices.Count == 0)
        {
            DrawCentered(dc, "No data yet", MutedBrush, 14, false, new Point(w / 2, h / 2));
            return;
        }

        switch (ChartType)
        {
            case ChartType.Donut: DrawDonut(dc, slices, w, h); break;
            case ChartType.Bars: DrawBars(dc, slices, w, h); break;
            case ChartType.Trend: DrawTrend(dc, slices, w, h); break;
        }
    }

    private void DrawDonut(DrawingContext dc, List<ChartSlice> slices, double w, double h)
    {
        var total = slices.Sum(s => s.Value);
        var cx = w / 2;
        var cy = h / 2;
        var outer = Math.Min(w, h) / 2 - 6;
        var inner = outer * 0.62;

        // background track
        var trackRadius = (outer + inner) / 2;
        dc.DrawEllipse(null, new Pen(TrackBrush, outer - inner), new Point(cx, cy), trackRadius, trackRadius);

        double startAngle = -90; // start at 12 o'clock
        foreach (var s in slices)
        {
            var sweep = s.Value / total * 360.0;
            if (sweep <= 0) continue;
            var geo = RingSegment(new Point(cx, cy), inner, outer, startAngle, startAngle + sweep);
            dc.DrawGeometry(new SolidColorBrush(s.Color), null, geo);
            startAngle += sweep;
        }

        if (!string.IsNullOrEmpty(CenterText))
            DrawCentered(dc, CenterText, TextBrush, 30, true, new Point(cx, cy - 8));
        if (!string.IsNullOrEmpty(CenterSubtext))
            DrawCentered(dc, CenterSubtext, MutedBrush, 12, false, new Point(cx, cy + 22));
    }

    private void DrawBars(DrawingContext dc, List<ChartSlice> slices, double w, double h)
    {
        const double labelBand = 30;
        var max = slices.Max(s => s.Value);
        var top = 12.0;
        var bottom = h - labelBand;
        var chartH = bottom - top;
        var n = slices.Count;
        var slot = w / n;
        var barW = Math.Min(46, slot * 0.6);

        for (var i = 0; i < n; i++)
        {
            var s = slices[i];
            var barH = max <= 0 ? 0 : s.Value / max * chartH;
            var x = i * slot + (slot - barW) / 2;
            var y = bottom - barH;
            var rect = new Rect(x, y, barW, Math.Max(2, barH));
            dc.DrawRoundedRectangle(new SolidColorBrush(s.Color), null, rect, 5, 5);

            DrawCentered(dc, TrimLabel(s.Label), MutedBrush, 11, false,
                new Point(x + barW / 2, bottom + 14));
        }
    }

    private void DrawTrend(DrawingContext dc, List<ChartSlice> slices, double w, double h)
    {
        const double labelBand = 26;
        var top = 14.0;
        var bottom = h - labelBand;
        var left = 8.0;
        var right = w - 8;
        var chartH = bottom - top;
        var chartW = right - left;
        var n = slices.Count;
        if (n < 2)
        {
            DrawBars(dc, slices, w, h);
            return;
        }

        var max = slices.Max(s => s.Value);
        Point PointFor(int i) => new(
            left + chartW * i / (n - 1),
            bottom - (max <= 0 ? 0 : slices[i].Value / max * chartH));

        // area fill
        var fig = new PathFigure { StartPoint = new Point(left, bottom), IsClosed = true };
        fig.Segments.Add(new LineSegment(PointFor(0), false));
        for (var i = 1; i < n; i++) fig.Segments.Add(new LineSegment(PointFor(i), false));
        fig.Segments.Add(new LineSegment(new Point(right, bottom), false));
        var area = new PathGeometry(new[] { fig });
        var accent = slices[0].Color;
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(46, accent.R, accent.G, accent.B)), null, area);

        // line
        var linePen = new Pen(new SolidColorBrush(accent), 2.5) { LineJoin = PenLineJoin.Round };
        for (var i = 1; i < n; i++) dc.DrawLine(linePen, PointFor(i - 1), PointFor(i));

        // dots + sparse labels
        var labelEvery = Math.Max(1, n / 6);
        for (var i = 0; i < n; i++)
        {
            var p = PointFor(i);
            dc.DrawEllipse(new SolidColorBrush(accent), null, p, 3, 3);
            if (i % labelEvery == 0 || i == n - 1)
                DrawCentered(dc, TrimLabel(slices[i].Label), MutedBrush, 10, false, new Point(p.X, bottom + 12));
        }
    }

    // --- helpers ---

    private static string TrimLabel(string s) => s.Length <= 8 ? s : s[..7] + "…";

    private void DrawCentered(DrawingContext dc, string text, Brush brush, double size, bool bold, Point center)
    {
        var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            bold ? FaceSemibold : Face, size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft, new Point(center.X - ft.Width / 2, center.Y - ft.Height / 2));
    }

    /// <summary>Build a filled ring (donut) segment between two radii and two angles (degrees).</summary>
    private static Geometry RingSegment(Point c, double rInner, double rOuter, double a0, double a1)
    {
        Point P(double r, double aDeg)
        {
            var a = aDeg * Math.PI / 180.0;
            return new Point(c.X + r * Math.Cos(a), c.Y + r * Math.Sin(a));
        }

        var large = (a1 - a0) > 180.0;
        var fig = new PathFigure { StartPoint = P(rOuter, a0), IsClosed = true };
        fig.Segments.Add(new ArcSegment(P(rOuter, a1), new Size(rOuter, rOuter), 0, large, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(P(rInner, a1), true));
        fig.Segments.Add(new ArcSegment(P(rInner, a0), new Size(rInner, rInner), 0, large, SweepDirection.Counterclockwise, true));
        var geo = new PathGeometry(new[] { fig });
        geo.Freeze();
        return geo;
    }
}
