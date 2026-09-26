using SubTrackr.Core.Contracts;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>A worth mode choice with its label.</summary>
public sealed record WorthOption(WorthMode Mode, string Label)
{
    public override string ToString() => Label;
}
