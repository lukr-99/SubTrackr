using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Updates;
using SubTrackr.Desktop.Composition;
using SubTrackr.Desktop.Tests.Fakes;

namespace SubTrackr.Desktop.Tests.Hosting;

/// <summary>
/// The whole app graph on fakes: an in-memory store with the first-run sample data (no websites, so
/// no logo is fetched), fixed rates, a fake cloud and release channel, and dialogs that answer on
/// their own.
/// </summary>
public sealed class TestApp : IDisposable
{
    public const string DataFolder = @"C:\Users\you\AppData\Roaming\SubTrackr";

    public static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    private TestApp(AppGraph graph, FakeTimeProvider time, InMemoryDatabaseStore store, RecordingDialogs dialogs, RecordingDesktop desktop, FakeUpdateChannel releases, FakeSyncProvider cloud, InMemoryBackupFiles files)
    {
        Graph = graph;
        Time = time;
        Store = store;
        Dialogs = dialogs;
        Desktop = desktop;
        Releases = releases;
        Cloud = cloud;
        Files = files;
    }

    public AppGraph Graph { get; }

    public FakeTimeProvider Time { get; }

    public InMemoryDatabaseStore Store { get; }

    public RecordingDialogs Dialogs { get; }

    public RecordingDesktop Desktop { get; }

    public FakeUpdateChannel Releases { get; }

    public FakeSyncProvider Cloud { get; }

    public InMemoryBackupFiles Files { get; }

    public static TestApp Create(Database? database = null, string version = "0.3.0", PublishedRelease? latest = null)
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryDatabaseStore(database ?? SampleDatabase());
        var dialogs = new RecordingDialogs();
        var desktop = new RecordingDesktop();
        var releases = new FakeUpdateChannel { Latest = latest };
        var cloud = new FakeSyncProvider();
        var files = new InMemoryBackupFiles();
        var adapters = new AppAdapters(time, store, new FixedRateProvider(), cloud, releases, releases, releases, files, new RecordingLog(), DataFolder);
        var graph = new AppGraph(new BuildInfo(version), adapters, dialogs, desktop);
        return new TestApp(graph, time, store, dialogs, desktop, releases, cloud, files);
    }

    /// <summary>The first-run data without websites, plus a budget, a trial ending soon, and a paused plan.</summary>
    public static Database SampleDatabase()
    {
        var database = SeedData.CreateInitialDatabase(Now);
        foreach (var subscription in database.Subscriptions)
        {
            subscription.Website = "";
        }

        database.Settings.MonthlyBudget = 2000;
        database.Subscriptions[2].TrialEnd = "2026-09-28";
        database.Subscriptions[4].Status = SubStatus.Paused;
        database.Subscriptions[1].WorthMode = WorthMode.Essential;
        database.Subscriptions[5].UsesPerMonth = 1;
        return database;
    }

    public void Dispose() => Graph.Dispose();
}
