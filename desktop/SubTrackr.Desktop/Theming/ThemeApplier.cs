using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SubTrackr.Core.Contracts;
using ThemeMode = SubTrackr.Core.Contracts.ThemeMode;

namespace SubTrackr.Desktop.Theming;

/// <summary>
/// Puts the active palette into the app's resources under the semantic keys the views read through
/// DynamicResource (<c>Bg</c>, <c>Surface</c>, <c>TextPrimary</c>, <c>Accent</c>, ...), so a switch
/// shows at once. Each switch stores fresh frozen brushes, because WPF keeps a brush it already
/// handed out. System mode follows Windows and re-applies when Windows changes.
/// </summary>
public sealed class ThemeApplier
{
    public const string ChartColorsKey = "ChartColors";

    private readonly ResourceDictionary resources;
    private readonly ISystemTheme system;
    private readonly Dispatcher? dispatcher;

    public ThemeApplier(DesignTokens tokens, ResourceDictionary resources, ISystemTheme system)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(system);
        Tokens = tokens;
        this.resources = resources;
        this.system = system;
        dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
        system.Changed += (_, _) => OnSystemChanged();
    }

    /// <summary>After every apply.</summary>
    public event EventHandler? Applied;

    public DesignTokens Tokens { get; }

    public ThemeMode Mode { get; private set; }

    public bool IsDark { get; private set; }

    public ThemePalette Palette => IsDark ? Tokens.Dark : Tokens.Light;

    public void Apply(ThemeMode mode)
    {
        Mode = mode;
        IsDark = mode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            _ => system.AppsUseDarkTheme,
        };

        var palette = Palette;
        var neutral = palette.Neutral;
        Set("Bg", neutral.Background);
        Set("Surface", neutral.Surface);
        Set("SurfaceAlt", neutral.SurfaceAlt);
        Set("Border", neutral.Border);
        Set("TextPrimary", neutral.TextPrimary);
        Set("TextSecondary", neutral.TextSecondary);
        Set("TextMuted", neutral.TextMuted);
        Set("Positive", neutral.Positive);
        Set("Negative", neutral.Negative);
        Set("Warning", neutral.Warning);
        Set("Accent", palette.Accent.Accent);
        Set("AccentHover", palette.Accent.AccentHover);
        Set("OnAccent", palette.Accent.OnAccent);
        resources[ChartColorsKey] = palette.Chart.ToArray();
        resources[TitleBarTheme.IsDarkResourceKey] = IsDark;

        foreach (var window in OpenWindows())
        {
            TitleBarTheme.Apply(window, IsDark);
        }

        Applied?.Invoke(this, EventArgs.Empty);
    }

    private void Set(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        resources[key] = brush;
        resources[key + "Color"] = color;
    }

    private void OnSystemChanged()
    {
        if (Mode != ThemeMode.System)
        {
            return;
        }

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Apply(ThemeMode.System);
        }
        else
        {
            dispatcher.BeginInvoke(() => Apply(ThemeMode.System));
        }
    }

    private IEnumerable<Window> OpenWindows() =>
        Application.Current is { } app && app.Dispatcher.CheckAccess() && ReferenceEquals(app.Resources, resources)
            ? app.Windows.Cast<Window>().ToList()
            : [];
}
