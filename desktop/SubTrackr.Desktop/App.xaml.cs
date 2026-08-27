using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop;

public partial class App : Application
{
    private const string InstanceName = "SubTrackr_SingleInstance_7f3a";
    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance: if SubTrackr is already running, just exit this copy.
        _instanceMutex = new Mutex(true, InstanceName, out var isPrimary);
        if (!isPrimary) { Shutdown(); return; }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error("Fatal unhandled exception", args.ExceptionObject as Exception);

        Log.Info($"SubTrackr starting (v{Updater.CurrentVersion}).");
        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        _ = CheckForUpdatesAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Silent best-effort update check on launch; offers to install if a newer build exists.</summary>
    private async Task CheckForUpdatesAsync()
    {
        var release = await Updater.CheckAsync();
        if (release is null) return;

        var choice = MessageBox.Show(
            $"SubTrackr {release.Version} is available (you have {Updater.CurrentVersion}).\n\nDownload and install now?",
            "Update available", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (choice != MessageBoxResult.Yes) return;

        try
        {
            await Updater.DownloadAndLaunchAsync(release);
            Shutdown(); // let the installer replace files
        }
        catch (Exception ex)
        {
            Log.Error("Update download/launch failed", ex);
            MessageBox.Show("Couldn't download the update. Please try again later.",
                "SubTrackr", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("Unhandled UI exception", e.Exception);
        MessageBox.Show(
            "SubTrackr hit an unexpected error and logged the details.\n\n" + e.Exception.Message,
            "SubTrackr", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
