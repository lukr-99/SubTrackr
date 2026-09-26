namespace SubTrackr.Desktop.Controls;

/// <summary>One datum for <see cref="SpendChart"/>: a labelled value and its place in the theme's chart colors.</summary>
public sealed class ChartSlice
{
    public required string Label { get; init; }
    public required double Value { get; init; }
    /// <summary>Which of the theme's chart colors to use; it wraps past the end of the list.</summary>
    public required int ColorIndex { get; init; }

    /// <summary>Optional secondary label (e.g. formatted amount) shown in legends.</summary>
    public string? ValueLabel { get; init; }
}
