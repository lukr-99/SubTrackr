using System.IO;
using System.Text.Json;
using System.Windows;

namespace SubTrackr.Desktop.Branding;

/// <summary>
/// The SubTrackr mark from contracts/design/logo.json (SPEC.md section 10), in units of a
/// <see cref="Canvas"/>-wide square: a rounded square with three rising bars, centered both ways.
/// The file is built into this assembly, like the color tokens.
/// </summary>
public sealed record LogoSpec(
    double Canvas,
    double CornerRadius,
    double BarWidth,
    double BarGap,
    double BarCornerRadius,
    double Baseline,
    IReadOnlyList<double> BarHeights,
    LogoAnimation Animation)
{
    public const string ResourceName = "SubTrackr.Contracts.logo.json";

    private static readonly Lazy<LogoSpec> Embedded = new(() =>
    {
        using var stream = typeof(LogoSpec).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The logo ({ResourceName}) is not built in.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    });

    /// <summary>The logo built into the app.</summary>
    public static LogoSpec LoadEmbedded() => Embedded.Value;

    public static LogoSpec Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("format").GetString() != "subtrackr-logo" || root.GetProperty("version").GetInt32() != 1)
        {
            throw new InvalidDataException("Not version 1 of the SubTrackr logo.");
        }

        var animation = root.GetProperty("animation");
        return new LogoSpec(
            root.GetProperty("canvas").GetDouble(),
            root.GetProperty("cornerRadius").GetDouble(),
            root.GetProperty("barWidth").GetDouble(),
            root.GetProperty("barGap").GetDouble(),
            root.GetProperty("barCornerRadius").GetDouble(),
            root.GetProperty("baseline").GetDouble(),
            [.. root.GetProperty("barHeights").EnumerateArray().Select(h => h.GetDouble())],
            new LogoAnimation(
                animation.GetProperty("barDurationMs").GetInt32(),
                animation.GetProperty("staggerMs").GetInt32(),
                animation.GetProperty("easing").GetString() ?? ""));
    }

    /// <summary>
    /// Bar <paramref name="index"/> at full height, standing on the baseline; the row of bars is
    /// centered horizontally.
    /// </summary>
    public Rect Bar(int index)
    {
        var rowWidth = (BarHeights.Count * BarWidth) + ((BarHeights.Count - 1) * BarGap);
        var left = ((Canvas - rowWidth) / 2) + (index * (BarWidth + BarGap));
        var height = BarHeights[index];
        return new Rect(left, Baseline - height, BarWidth, height);
    }
}
