using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Shell;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;
using SubTrackr.Desktop.Views;
using ThemeMode = SubTrackr.Core.Contracts.ThemeMode;

namespace SubTrackr.Desktop.Tests.Ui;

/// <summary>
/// Opens the real main window far off screen with sample data, shows every page and the edit form
/// in the light and the dark theme, and fails on any binding error. With SUBTRACKR_SCREENSHOTS set
/// to a folder it also saves each page as a PNG there, plus the whole scrolled settings page.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class PageRenderTests
{
    private static readonly AppPage[] Pages = [AppPage.Dashboard, AppPage.WhatIf, AppPage.Settings];

    [Fact]
    public Task EveryPage_BothThemes_RendersWithoutBindingErrors() => WpfHost.RunAsync(async () =>
    {
        using var errors = new BindingErrorRecorder();
        using var app = TestApp.Create(resources: Application.Current.Resources);
        await app.Graph.Rates.RefreshAsync(app.Graph.Ledger.BaseCurrency, CancellationToken.None);

        var window = OffScreen(new MainWindow(app.Graph.Main), 1280, 820);
        window.Show();
        try
        {
            foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                // Through the settings picker, the way a person switches.
                app.Graph.Settings.SelectedTheme = app.Graph.Settings.ThemeOptions.Single(o => o.Mode == mode);
                var suffix = "-" + mode.ToString().ToLowerInvariant();
                foreach (var page in Pages)
                {
                    app.Graph.Main.Open(page);
                    await Settle();
                    Capture(window, page.ToString().ToLowerInvariant() + suffix);
                }

                var settings = Find<SettingsView>(window);
                CaptureWhole((FrameworkElement)((ScrollViewer)settings.Content).Content, window.Background, "settings-full" + suffix);

                var editor = new EditSubscriptionViewModel(new DateOnly(2026, 9, 26), app.Graph.Ledger.Subscriptions[2]);
                var edit = OffScreen(new EditSubscriptionWindow(editor), 460, double.NaN);
                edit.Show();
                try
                {
                    await Settle();
                    Capture(edit, "edit" + suffix);
                }
                finally
                {
                    edit.Close();
                }
            }
        }
        finally
        {
            window.Close();
        }

        Assert.True(errors.Errors.Count == 0, string.Join(Environment.NewLine, errors.Errors));
    });

    [Fact]
    public Task ThemeSwitch_RepaintsAnOpenWindow() => WpfHost.RunAsync(async () =>
    {
        using var app = TestApp.Create(resources: Application.Current.Resources);
        var window = OffScreen(new MainWindow(app.Graph.Main), 1280, 820);
        window.Show();
        try
        {
            app.Graph.Theme.Apply(ThemeMode.Light);
            await Settle();
            Assert.Equal(app.Graph.Theme.Tokens.Light.Neutral.Background, ((SolidColorBrush)window.Background).Color);

            app.Graph.Theme.Apply(ThemeMode.Dark);
            await Settle();
            Assert.Equal(app.Graph.Theme.Tokens.Dark.Neutral.Background, ((SolidColorBrush)window.Background).Color);
        }
        finally
        {
            window.Close();
        }
    });

    private static T OffScreen<T>(T window, double width, double height)
        where T : Window
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -30_000;
        window.Top = -30_000;
        window.Width = width;
        if (!double.IsNaN(height))
        {
            window.Height = height;
        }

        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        return window;
    }

    private static async Task Settle()
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await Task.Delay(100);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    private static T Find<T>(DependencyObject root)
        where T : DependencyObject =>
        FindOrNull<T>(root) ?? throw new InvalidOperationException($"No {typeof(T).Name} in the tree.");

    private static T? FindOrNull<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var match = child as T ?? FindOrNull<T>(child);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    // The window's template root paints the window background, so the PNG shows what a person sees.
    private static void Capture(Window window, string name)
    {
        var element = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        Save(bitmap, name);
    }

    // An element taller than the window (a page's scrolled content), on the window background.
    private static void CaptureWhole(FrameworkElement element, Brush background, string name)
    {
        element.UpdateLayout();
        var bounds = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(background, null, bounds);
            drawing.DrawRectangle(new VisualBrush(element), null, bounds);
        }

        var bitmap = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        Save(bitmap, name);
    }

    private static void Save(BitmapSource bitmap, string name)
    {
        Assert.True(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0);
        var folder = Environment.GetEnvironmentVariable("SUBTRACKR_SCREENSHOTS");
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(folder, name + ".png"));
        encoder.Save(output);
    }
}
