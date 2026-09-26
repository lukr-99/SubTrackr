using System.Windows.Media;

namespace SubTrackr.Desktop.Theming;

/// <summary>
/// Surfaces, text, borders, and status colors for one mode (the <c>neutral</c> group of
/// contracts/design/tokens.json). Accents live apart in <see cref="AccentPalette"/>.
/// </summary>
public sealed record NeutralPalette(
    Color Background,
    Color Surface,
    Color SurfaceAlt,
    Color Border,
    Color TextPrimary,
    Color TextSecondary,
    Color TextMuted,
    Color Positive,
    Color Negative,
    Color Warning);
