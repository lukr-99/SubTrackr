using System.Windows.Media;

namespace SubTrackr.Desktop.Controls;

public enum ChartType
{
    Donut,
    Bars,
    Trend,
}

/// <summary>One datum for <see cref="SpendChart"/> — a labelled value with a colour.</summary>
public sealed class ChartSlice
{
    public required string Label { get; init; }
    public required double Value { get; init; }
    public required Color Color { get; init; }

    /// <summary>Optional secondary label (e.g. formatted amount) shown in legends.</summary>
    public string? ValueLabel { get; init; }
}
