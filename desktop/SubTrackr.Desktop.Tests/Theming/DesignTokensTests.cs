using System.Text.Json;
using System.Windows.Media;
using SubTrackr.Desktop.Theming;

namespace SubTrackr.Desktop.Tests.Theming;

/// <summary>
/// The app's palettes must equal contracts/design/tokens.json (SPEC.md section 10). The test reads
/// the contract file itself and compares every token with what the app built in.
/// </summary>
public class DesignTokensTests
{
    private static readonly string TokensPath = Path.Combine(AppContext.BaseDirectory, "design", "tokens.json");

    public static IEnumerable<object[]> Modes() => [["light"], ["dark"]];

    [Theory]
    [MemberData(nameof(Modes))]
    public void EmbeddedPalette_EqualsTheContractFile(string mode)
    {
        using var file = JsonDocument.Parse(File.ReadAllText(TokensPath));
        var palette = mode == "dark" ? DesignTokens.LoadEmbedded().Dark : DesignTokens.LoadEmbedded().Light;

        Assert.Equal(Expected(file, "neutral", mode), Actual(palette.Neutral));
        Assert.Equal(Expected(file, "accent", mode), Actual(palette.Accent));
        Assert.Equal(
            file.RootElement.GetProperty("chart").GetProperty(mode).EnumerateArray().Select(c => c.GetString()!).ToList(),
            palette.Chart.Select(Hex).ToList());
    }

    [Theory]
    [InlineData("#1A1B1E")]
    [InlineData("#ffffff")]
    public void ParseColor_ReadsHex(string hex)
    {
        Assert.Equal(hex.ToUpperInvariant(), Hex(DesignTokens.ParseColor(hex)));
    }

    [Theory]
    [InlineData("1A1B1E")]
    [InlineData("#1A1B1")]
    [InlineData("#GGGGGG")]
    public void ParseColor_RejectsOtherText(string text)
    {
        Assert.Throws<InvalidDataException>(() => DesignTokens.ParseColor(text));
    }

    internal static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Dictionary<string, string> Expected(JsonDocument file, string group, string mode) =>
        file.RootElement.GetProperty(group).GetProperty(mode).EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!.ToUpperInvariant());

    private static Dictionary<string, string> Actual(NeutralPalette p) => new()
    {
        ["background"] = Hex(p.Background),
        ["surface"] = Hex(p.Surface),
        ["surfaceAlt"] = Hex(p.SurfaceAlt),
        ["border"] = Hex(p.Border),
        ["textPrimary"] = Hex(p.TextPrimary),
        ["textSecondary"] = Hex(p.TextSecondary),
        ["textMuted"] = Hex(p.TextMuted),
        ["positive"] = Hex(p.Positive),
        ["negative"] = Hex(p.Negative),
        ["warning"] = Hex(p.Warning),
    };

    private static Dictionary<string, string> Actual(AccentPalette p) => new()
    {
        ["accent"] = Hex(p.Accent),
        ["accentHover"] = Hex(p.AccentHover),
        ["onAccent"] = Hex(p.OnAccent),
    };
}
