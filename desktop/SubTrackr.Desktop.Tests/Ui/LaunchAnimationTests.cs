using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Shell;
using SubTrackr.Desktop.Tests.Hosting;

namespace SubTrackr.Desktop.Tests.Ui;

/// <summary>
/// The rail logo plays its launch animation when the main window first loads, and not at all when
/// Windows asks for reduced motion (SPEC.md section 10). Windows open far off screen.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class LaunchAnimationTests
{
    [Fact]
    public Task ReducedMotion_ShowsTheFinishedLogoWithoutAnimating() => WpfHost.RunAsync(async () =>
    {
        using var app = TestApp.Create(resources: Application.Current.Resources);
        app.Motion.AnimationsEnabled = false;
        var window = OffScreen(new MainWindow(app.Graph.Main, app.Motion));
        window.Show();
        try
        {
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            Assert.True(window.IsLoaded);
            Assert.False(window.Logo.HasAnimatedProperties);
            Assert.Equal(1.0, window.Logo.Progress);
            Assert.All(window.Logo.BarProgress(), p => Assert.Equal(1.0, p));
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public Task Motion_GrowsTheBarsOnFirstShowThenLeavesTheLogoWhole() => WpfHost.RunAsync(async () =>
    {
        using var app = TestApp.Create(resources: Application.Current.Resources);
        app.Motion.AnimationsEnabled = true;
        var window = OffScreen(new MainWindow(app.Graph.Main, app.Motion));
        window.Show();
        try
        {
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(window.Logo.HasAnimatedProperties);

            await Task.Delay(window.Logo.AnimationLength + TimeSpan.FromMilliseconds(400));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(1.0, window.Logo.Progress);
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public Task LaunchFrames_Render() => WpfHost.RunAsync(() =>
    {
        // A strip of the mark part way through the animation, to look at with SUBTRACKR_SCREENSHOTS.
        var strip = new StackPanel { Orientation = Orientation.Horizontal, Background = Brushes.White };
        foreach (var progress in new[] { 0.0, 0.15, 0.3, 0.45, 0.6, 0.8, 1.0 })
        {
            strip.Children.Add(new LogoMark { Width = 96, Height = 96, Margin = new Thickness(8), Progress = progress });
        }

        strip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        strip.Arrange(new Rect(strip.DesiredSize));
        var bitmap = new RenderTargetBitmap((int)strip.ActualWidth, (int)strip.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(strip);
        Screenshots.Save(bitmap, "logo-launch-frames");

        var first = (LogoMark)strip.Children[0];
        Assert.All(first.BarProgress(), p => Assert.Equal(0.0, p));
    });

    private static MainWindow OffScreen(MainWindow window)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -30_000;
        window.Top = -30_000;
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        return window;
    }
}
