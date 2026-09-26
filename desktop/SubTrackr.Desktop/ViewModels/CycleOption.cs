using SubTrackr.Core.Contracts;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>A billing cycle choice with its label.</summary>
public sealed record CycleOption(BillingCycle Cycle, string Label)
{
    // The ComboBox renders items via ToString(); show the friendly label, not the record dump.
    public override string ToString() => Label;
}
