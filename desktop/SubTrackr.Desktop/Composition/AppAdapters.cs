using System.Net;
using System.Net.Http;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Sync;
using SubTrackr.Desktop.Services;
using SubTrackr.Infrastructure.Currency;
using SubTrackr.Infrastructure.Diagnostics;
using SubTrackr.Infrastructure.Storage;
using SubTrackr.Infrastructure.Sync;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// Everything that touches the disk, the network, or the clock, in one place, so the composition
/// root runs on the real ones (<see cref="ForUser"/>) or on fakes in tests. Owns and disposes the
/// HTTP clients it creates.
/// </summary>
public sealed class AppAdapters : IDisposable
{
    private readonly IDisposable[] owned;

    public AppAdapters(
        TimeProvider time,
        IDatabaseStore store,
        IRateProvider rates,
        ISyncProviderFactory syncProviders,
        IUpdater updater,
        IAppLog log,
        string dataFolder,
        params IDisposable[] owned)
    {
        Time = time;
        Store = store;
        Rates = rates;
        SyncProviders = syncProviders;
        Updater = updater;
        Log = log;
        DataFolder = dataFolder;
        this.owned = owned;
    }

    public TimeProvider Time { get; }

    public IDatabaseStore Store { get; }

    public IRateProvider Rates { get; }

    public ISyncProviderFactory SyncProviders { get; }

    public IUpdater Updater { get; }

    public IAppLog Log { get; }

    public string DataFolder { get; }

    /// <summary>The real adapters, with data in the user's app data folder.</summary>
    public static AppAdapters ForUser(BuildInfo build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var paths = AppDataPaths.ForUser();
        var time = TimeProvider.System;
        var web = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        web.DefaultRequestHeaders.UserAgent.ParseAdd($"SubTrackr/{build.CoreVersion}");

        return new AppAdapters(
            time,
            new JsonDatabaseStore(paths.DataFile),
            new FrankfurterRateProvider(web),
            new SupabaseSyncProviderFactory(web),
            new Updater(web, build.CoreVersion),
            new FileLog(paths.LogsFolder, time),
            paths.Root,
            web);
    }

    public void Dispose()
    {
        foreach (var disposable in owned)
        {
            disposable.Dispose();
        }
    }
}
