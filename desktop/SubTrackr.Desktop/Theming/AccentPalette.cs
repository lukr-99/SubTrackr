using System.Windows.Media;

namespace SubTrackr.Desktop.Theming;

/// <summary>The accent for one mode (the <c>accent</c> group of contracts/design/tokens.json).</summary>
public sealed record AccentPalette(Color Accent, Color AccentHover, Color OnAccent);
