using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace SubTrackr.Desktop.Theming;

/// <summary>
/// The light and dark palettes from contracts/design/tokens.json, the single source of SubTrackr's
/// colors (SPEC.md section 10). The file is built into this assembly, so the app cannot run
/// without it or with a stale copy.
/// </summary>
public sealed record DesignTokens(ThemePalette Light, ThemePalette Dark)
{
    public const string ResourceName = "SubTrackr.Contracts.tokens.json";

    /// <summary>The tokens built into the app.</summary>
    public static DesignTokens LoadEmbedded()
    {
        using var stream = typeof(DesignTokens).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The design tokens ({ResourceName}) are not built in.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static DesignTokens Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("format").GetString() != "subtrackr-design-tokens" || root.GetProperty("version").GetInt32() != 1)
        {
            throw new InvalidDataException("Not version 1 of the SubTrackr design tokens.");
        }

        return new DesignTokens(Palette(root, "light"), Palette(root, "dark"));
    }

    private static ThemePalette Palette(JsonElement root, string mode)
    {
        var neutral = root.GetProperty("neutral").GetProperty(mode);
        var accent = root.GetProperty("accent").GetProperty(mode);
        return new ThemePalette(
            new NeutralPalette(
                Hex(neutral, "background"),
                Hex(neutral, "surface"),
                Hex(neutral, "surfaceAlt"),
                Hex(neutral, "border"),
                Hex(neutral, "textPrimary"),
                Hex(neutral, "textSecondary"),
                Hex(neutral, "textMuted"),
                Hex(neutral, "positive"),
                Hex(neutral, "negative"),
                Hex(neutral, "warning")),
            new AccentPalette(Hex(accent, "accent"), Hex(accent, "accentHover"), Hex(accent, "onAccent")),
            root.GetProperty("chart").GetProperty(mode).EnumerateArray().Select(c => ParseColor(c.GetString())).ToList());
    }

    private static Color Hex(JsonElement group, string name) => ParseColor(group.GetProperty(name).GetString());

    /// <summary>Reads <c>#RRGGBB</c>.</summary>
    public static Color ParseColor(string? hex)
    {
        if (hex is not { Length: 7 } || hex[0] != '#' || !uint.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new InvalidDataException($"'{hex}' is not a #RRGGBB color.");
        }

        return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
