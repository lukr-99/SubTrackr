using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.Theming;
using ThemeMode = SubTrackr.Core.Contracts.ThemeMode;

namespace SubTrackr.Desktop.Tests.Theming;

public class ThemeApplierTests
{
    private static readonly string TokensPath = Path.Combine(AppContext.BaseDirectory, "design", "tokens.json");

    private static readonly Dictionary<string, (string Group, string Token)> Keys = new()
    {
        ["Bg"] = ("neutral", "background"),
        ["Surface"] = ("neutral", "surface"),
        ["SurfaceAlt"] = ("neutral", "surfaceAlt"),
        ["Border"] = ("neutral", "border"),
        ["TextPrimary"] = ("neutral", "textPrimary"),
        ["TextSecondary"] = ("neutral", "textSecondary"),
        ["TextMuted"] = ("neutral", "textMuted"),
        ["Positive"] = ("neutral", "positive"),
        ["Negative"] = ("neutral", "negative"),
        ["Warning"] = ("neutral", "warning"),
        ["Accent"] = ("accent", "accent"),
        ["AccentHover"] = ("accent", "accentHover"),
        ["OnAccent"] = ("accent", "onAccent"),
    };

    private readonly FakeSystemTheme system = new();
    private readonly ResourceDictionary resources = new();

    [Theory]
    [InlineData(ThemeMode.Light, "light")]
    [InlineData(ThemeMode.Dark, "dark")]
    public void Apply_PutsTheContractColorsUnderTheSemanticKeys(ThemeMode mode, string tokens)
    {
        using var file = JsonDocument.Parse(File.ReadAllText(TokensPath));
        var applier = new ThemeApplier(DesignTokens.LoadEmbedded(), resources, system);

        applier.Apply(mode);

        foreach (var (key, (group, token)) in Keys)
        {
            var expected = file.RootElement.GetProperty(group).GetProperty(tokens).GetProperty(token).GetString()!.ToUpperInvariant();
            var brush = Assert.IsType<SolidColorBrush>(resources[key]);
            Assert.True(brush.IsFrozen, key);
            Assert.Equal(expected, DesignTokensTests.Hex(brush.Color));
            Assert.Equal(brush.Color, resources[key + "Color"]);
        }

        var chart = Assert.IsType<Color[]>(resources[ThemeApplier.ChartColorsKey]);
        Assert.Equal(
            file.RootElement.GetProperty("chart").GetProperty(tokens).EnumerateArray().Select(c => c.GetString()!.ToUpperInvariant()),
            chart.Select(DesignTokensTests.Hex));
        Assert.Equal(mode == ThemeMode.Dark, resources[TitleBarTheme.IsDarkResourceKey]);
    }

    [Fact]
    public void Apply_System_FollowsWindowsAndReactsWhenItChanges()
    {
        var applier = new ThemeApplier(DesignTokens.LoadEmbedded(), resources, system);
        var applied = 0;
        applier.Applied += (_, _) => applied++;

        applier.Apply(ThemeMode.System);
        Assert.False(applier.IsDark);

        system.Switch(dark: true);

        Assert.True(applier.IsDark);
        Assert.Equal(applier.Tokens.Dark.Neutral.Background, ((SolidColorBrush)resources["Bg"]).Color);
        Assert.Equal(2, applied);
    }

    [Fact]
    public void Apply_ExplicitMode_IgnoresWindowsChanges()
    {
        var applier = new ThemeApplier(DesignTokens.LoadEmbedded(), resources, system);
        applier.Apply(ThemeMode.Light);

        system.Switch(dark: true);

        Assert.False(applier.IsDark);
    }

    [Fact]
    public void Apply_Switch_StoresFreshBrushes()
    {
        var applier = new ThemeApplier(DesignTokens.LoadEmbedded(), resources, system);
        applier.Apply(ThemeMode.Dark);
        var dark = resources["Surface"];

        applier.Apply(ThemeMode.Light);

        Assert.NotSame(dark, resources["Surface"]);
    }
}
