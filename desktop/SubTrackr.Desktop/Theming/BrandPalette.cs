using System.Windows.Media;

namespace SubTrackr.Desktop.Theming;

/// <summary>
/// The logo's colors (the <c>brand</c> group of contracts/design/tokens.json), the same in both
/// themes: a gradient from <see cref="GradientStart"/> at the top left to <see cref="GradientEnd"/>
/// at the bottom right, with the bars in <see cref="Mark"/>.
/// </summary>
public sealed record BrandPalette(Color GradientStart, Color GradientEnd, Color Mark);
