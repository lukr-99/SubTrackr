using System.IO;
using System.Net;
using System.Net.Http;
using SubTrackr.Core.Backup;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Sync;
using SubTrackr.Core.Updates;
using SubTrackr.Desktop.Theming;
using SubTrackr.Infrastructure.Backup;
using SubTrackr.Infrastructure.Currency;
using SubTrackr.Infrastructure.Diagnostics;
using SubTrackr.Infrastructure.Storage;
using SubTrackr.Infrastructure.Sync;
using SubTrackr.Infrastructure.Updates;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// Everything that touches the disk, the network, the clock, or other processes, in one place, so
/// the composition root runs on the real ones (<see cref="ForUser"/>) or on fakes in tests. Owns
/// and disposes the HTTP clients it creates.
/// </summary>
public sealed class AppAdapters : IDisposable
{
    public const string ReleaseOwner = "lukr-99";
    public const string ReleaseRepository = "SubTrackr";

    private readonly IDisposable[] owned;

    public AppAdapters(
        TimeProvider time,
        IDatabaseStore store,
        IRateProvider rates,
        ISyncProviderFactory syncProviders,
        IReleaseSource releases,
        IUpdateDownloader downloads,
        IInstallerLauncher installer,
        IBackupFiles backupFiles,
        ISystemTheme systemTheme,
        IAppLog log,
        string dataFolder,
        params IDisposable[] owned)
    {
        Time = time;
        Store = store;
        Rates = rates;
        SyncProviders = syncProviders;
        Releases = releases;
        Downloads = downloads;
        Installer = installer;
        BackupFiles = backupFiles;
        SystemTheme = systemTheme;
        Log = log;
        DataFolder = dataFolder;
        this.owned = owned;
    }

    public TimeProvider Time { get; }

    public IDatabaseStore Store { get; }

    public IRateProvider Rates { get; }

    public ISyncProviderFactory SyncProviders { get; }

    public IReleaseSource Releases { get; }

    public IUpdateDownloader Downloads { get; }

    public IInstallerLauncher Installer { get; }

    public IBackupFiles BackupFiles { get; }

    public ISystemTheme SystemTheme { get; }

    public IAppLog Log { get; }

    public string DataFolder { get; }

    /// <summary>The real adapters, with data in the user's app data folder.</summary>
    public static AppAdapters ForUser(BuildInfo build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var paths = AppDataPaths.ForUser(build.IsDevBuild);
        var time = TimeProvider.System;
        var userAgent = $"SubTrackr/{build.Version}";

        // API calls: 15 seconds each (SPEC.md section 8.4).
        var web = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        web.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        // Installer downloads take longer; cancellation and the size cap bound them instead.
        var downloads = new HttpClient(new SocketsHttpHandler()) { Timeout = TimeSpan.FromMinutes(10) };
        downloads.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        var systemTheme = new WindowsSystemTheme();

        return new AppAdapters(
            time,
            new JsonDatabaseStore(paths.DataFile),
            new FrankfurterRateProvider(web),
            new SupabaseSyncProviderFactory(web),
            new GitHubReleaseSource(web, ReleaseOwner, ReleaseRepository),
            new VerifiedDownloader(downloads, Path.Combine(Path.GetTempPath(), "SubTrackr", "updates")),
            new InnoSetupLauncher(),
            new BackupFiles(),
            systemTheme,
            new FileLog(paths.LogsFolder, time),
            paths.Root,
            web,
            downloads,
            systemTheme);
    }

    public void Dispose()
    {
        foreach (var disposable in owned)
        {
            disposable.Dispose();
        }
    }
}
