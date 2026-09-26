using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SubTrackr.Desktop.Branding;
using SubTrackr.Desktop.Theming;

namespace SubTrackr.Desktop.Controls;

/// <summary>
/// The SubTrackr mark, drawn as vectors from the built-in logo.json and the <c>brand</c> colors of
/// tokens.json, the same in both themes. It fills the largest centered square that fits.
/// <see cref="PlayLaunchAnimation"/> grows the bars from their baseline one after another.
/// </summary>
public sealed class LogoMark : FrameworkElement
{
    /// <summary>
    /// How far the launch animation has run, 0 to 1 over its whole length; 1 (the default) is the
    /// finished mark.
    /// </summary>
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(LogoMark),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Lazy<BrandPalette> EmbeddedBrand = new(() => DesignTokens.LoadEmbedded().Brand);

    public LogoSpec Spec { get; } = LogoSpec.LoadEmbedded();

    public BrandPalette Brand { get; } = EmbeddedBrand.Value;

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>The launch animation's whole length, from logo.json.</summary>
    public TimeSpan AnimationLength => Spec.Animation.Total(Spec.BarHeights.Count);

    /// <summary>How far each bar has grown at <see cref="Progress"/>.</summary>
    public IReadOnlyList<double> BarProgress()
    {
        var elapsedMs = Math.Clamp(Progress, 0, 1) * AnimationLength.TotalMilliseconds;
        return [.. Enumerable.Range(0, Spec.BarHeights.Count).Select(i => Spec.Animation.BarProgress(i, elapsedMs))];
    }

    /// <summary>What <see cref="OnRender"/> draws, in <see cref="LogoSpec.Canvas"/> units.</summary>
    public DrawingGroup CreateDrawing() => LogoDrawing.Create(Spec, Brand, BarProgress());

    /// <summary>Runs <see cref="Progress"/> from 0 to 1; the mark is whole again when it ends.</summary>
    public void PlayLaunchAnimation() =>
        BeginAnimation(ProgressProperty, new DoubleAnimation(0, 1, new Duration(AnimationLength)) { FillBehavior = FillBehavior.Stop });

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
