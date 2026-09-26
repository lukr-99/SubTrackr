using System.Windows;
using System.Windows.Media;
using SubTrackr.Desktop.Branding;
using SubTrackr.Desktop.Theming;

namespace SubTrackr.Desktop.Controls;

/// <summary>
/// The SubTrackr mark, drawn as vectors from the built-in logo.json and the <c>brand</c> colors of
/// tokens.json, the same in both themes. It fills the largest centered square that fits.
/// </summary>
public sealed class LogoMark : FrameworkElement
{
    private static readonly Lazy<BrandPalette> EmbeddedBrand = new(() => DesignTokens.LoadEmbedded().Brand);

    public LogoSpec Spec { get; } = LogoSpec.LoadEmbedded();

    public BrandPalette Brand { get; } = EmbeddedBrand.Value;

    /// <summary>What <see cref="OnRender"/> draws, in <see cref="LogoSpec.Canvas"/> units.</summary>
    public DrawingGroup CreateDrawing() =>
        LogoDrawing.Create(Spec, Brand, [.. Spec.BarHeights.Select(_ => 1.0)]);

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
        {
            return;
        }

        var scale = size / Spec.Canvas;
        drawingContext.PushTransform(new TranslateTransform((ActualWidth - size) / 2, (ActualHeight - size) / 2));
        drawingContext.PushTransform(new ScaleTransform(scale, scale));
        drawingContext.DrawDrawing(CreateDrawing());
        drawingContext.Pop();
        drawingContext.Pop();
    }
}
