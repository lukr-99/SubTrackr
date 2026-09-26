namespace SubTrackr.Desktop.ViewModels;

/// <summary>One upcoming renewal.</summary>
public sealed record RenewalLine(string Icon, string Name, string WhenText, string AmountText, bool Soon);
