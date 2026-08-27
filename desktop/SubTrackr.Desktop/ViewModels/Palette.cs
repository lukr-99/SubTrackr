using System.Windows.Media;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>Categorical colour palette for charts (mirrors Themes/Dark.xaml Cat0..7).</summary>
public static class Palette
{
    public static readonly Color[] Colors =
    {
        Color.FromRgb(0x4C, 0x8D, 0xFF), // blue
        Color.FromRgb(0x3D, 0xD6, 0x8C), // green
        Color.FromRgb(0xFF, 0xC0, 0x48), // amber
        Color.FromRgb(0xFF, 0x6B, 0x6B), // red
        Color.FromRgb(0xB0, 0x84, 0xFF), // purple
        Color.FromRgb(0x4C, 0xD4, 0xE0), // cyan
        Color.FromRgb(0xFF, 0x9F, 0x6B), // orange
        Color.FromRgb(0xE8, 0x6B, 0xC7), // pink
    };

    public static Color At(int i) => Colors[((i % Colors.Length) + Colors.Length) % Colors.Length];
}
