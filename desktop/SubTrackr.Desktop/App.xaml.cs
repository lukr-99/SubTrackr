using System.Windows;
using System.Windows.Threading;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Desktop.Composition;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.Shell;

namespace SubTrackr.Desktop;

/// <summary>Process lifetime: single instance, the composition root, then the main window.</summary>
public partial class App : Application
{
    private Mutex? instance;
    private AppGraph? graph;
    private IAppLog? log;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance per build kind: a second copy just exits. Dev builds use their own mutex,
        // so they run beside the installed release.
        var build = BuildInfo.FromAssembly(typeof(App).Assembly);
        instance = new Mutex(true, build.InstanceMutexName, out var isPrimary);
        if (!isPrimary)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
        var adapters = AppAdapters.ForUser(build);
        log = adapters.Log;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            log.Error("Fatal unhandled exception", args.ExceptionObject as Exception);
        log.Info($"SubTrackr starting (v{build.Version}).");

        graph = new AppGraph(build, adapters, new WpfDialogService(), new WpfDesktopServices());
        var window = new MainWindow(graph.Main);
        MainWindow = window;
        window.Show();
        graph.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        graph?.Dispose();
        instance?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        log?.Error("Unhandled UI exception", e.Exception);
        MessageBox.Show(
            "SubTrackr hit an unexpected error and logged the details.\n\n" + e.Exception.Message,
            "SubTrackr", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
