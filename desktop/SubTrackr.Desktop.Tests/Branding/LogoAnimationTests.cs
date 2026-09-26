using System.Text.Json;
using SubTrackr.Desktop.Branding;

namespace SubTrackr.Desktop.Tests.Branding;

/// <summary>The launch timing must be the one in contracts/design/logo.json (SPEC.md section 10).</summary>
public class LogoAnimationTests
{
    private static readonly string LogoPath = Path.Combine(AppContext.BaseDirectory, "design", "logo.json");

    private static readonly LogoSpec Spec = LogoSpec.LoadEmbedded();

    [Fact]
    public void EmbeddedTiming_EqualsTheContractFile()
    {
        using var file = JsonDocument.Parse(File.ReadAllText(LogoPath));
        var animation = file.RootElement.GetProperty("animation");

        Assert.Equal(
            new LogoAnimation(
                animation.GetProperty("barDurationMs").GetInt32(),
                animation.GetProperty("staggerMs").GetInt32(),
                animation.GetProperty("easing").GetString()!),
            Spec.Animation);
    }

    [Fact]
    public void Total_RunsFromTheFirstBarsStartToTheLastBarsEnd()
    {
        var a = Spec.Animation;
        Assert.Equal(TimeSpan.FromMilliseconds(a.BarDurationMs + (2 * a.StaggerMs)), a.Total(3));
    }

    [Fact]
    public void Bars_StartOneAfterAnotherFromTheBaseline()
    {
        var a = Spec.Animation;
        Assert.All(Enumerable.Range(0, 3), i => Assert.Equal(0, a.BarProgress(i, 0)));

        // One stagger in: the first bar is growing, the second has not started.
        Assert.InRange(a.BarProgress(0, a.StaggerMs), 0.01, 0.99);
        Assert.Equal(0, a.BarProgress(1, a.StaggerMs));

        // Each bar finishes exactly one bar duration after it starts.
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(1, a.BarProgress(i, (i * a.StaggerMs) + a.BarDurationMs));
        }

        // Earlier bars are always at least as far along.
        for (var t = 0.0; t <= a.Total(3).TotalMilliseconds; t += 10)
        {
            Assert.True(a.BarProgress(0, t) >= a.BarProgress(1, t) && a.BarProgress(1, t) >= a.BarProgress(2, t));
        }
    }

    [Fact]
    public void Decelerate_IsFastFirstThenSlows()
    {
        var a = Spec.Animation;
        Assert.Equal(0.75, a.Ease(0.5), 10);
        Assert.True(a.Ease(0.25) - a.Ease(0) > a.Ease(1) - a.Ease(0.75));
    }

    [Fact]
    public void UnknownEasing_IsRejected()
    {
        Assert.Throws<InvalidDataException>(() => new LogoAnimation(320, 110, "bounce").Ease(0.5));
    }
}
