using System.IO;

namespace SubTrackr.Desktop.Branding;

/// <summary>
/// The launch animation from contracts/design/logo.json: bar <c>i</c> starts growing from its
/// baseline <c>i * StaggerMs</c> after the start and takes <see cref="BarDurationMs"/>, eased with
/// <see cref="Easing"/>.
/// </summary>
public sealed record LogoAnimation(int BarDurationMs, int StaggerMs, string Easing)
{
    public const string Decelerate = "decelerate";

    /// <summary>From the start of the first bar to the end of the last one.</summary>
    public TimeSpan Total(int barCount) =>
        TimeSpan.FromMilliseconds(BarDurationMs + (StaggerMs * Math.Max(0, barCount - 1)));

    /// <summary>How far bar <paramref name="index"/> has grown, 0 to 1, at <paramref name="elapsedMs"/>.</summary>
    public double BarProgress(int index, double elapsedMs) =>
        Ease(Math.Clamp((elapsedMs - (index * (double)StaggerMs)) / BarDurationMs, 0, 1));

    /// <summary>Decelerate: fast at first, slowing to a stop (1 - (1 - t)^2).</summary>
    public double Ease(double linear) => Easing switch
    {
        Decelerate => 1 - ((1 - linear) * (1 - linear)),
        _ => throw new InvalidDataException($"Unknown logo easing '{Easing}'."),
    };
}
