using SubTrackr.Core.Contracts;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>A theme mode choice with its label.</summary>
public sealed record ThemeOption(ThemeMode Mode, string Label)
{
    public override string ToString() => Label;
}
