using System.Windows.Media;

namespace SubTrackr.Desktop.Theming;

/// <summary>Everything one mode paints with: neutrals, the accent, and the chart series colors.</summary>
public sealed record ThemePalette(NeutralPalette Neutral, AccentPalette Accent, IReadOnlyList<Color> Chart);
