using System.Windows;
using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Updates;
using SubTrackr.Desktop.Composition;
using SubTrackr.Desktop.Tests.Fakes;

namespace SubTrackr.Desktop.Tests.Hosting;

/// <summary>
/// The whole app graph on fakes: an in-memory store with the first-run sample data (no websites, so
/// no logo is fetched), fixed rates, a fake cloud and release channel, a Windows theme the test
/// sets, and dialogs that answer on their own. View model tests theme a private resource
/// dictionary; render tests pass the WPF host's application resources.
/// </summary>
public sealed class TestApp : IDisposable
{
    public const string DataFolder = @"C:\Users\you\AppData\Roaming\SubTrackr";

    public static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    private TestApp()
    {
    }

    public required AppGraph Graph { get; init; }

    public required FakeTimeProvider Time { get; init; }

    public required InMemoryDatabaseStore Store { get; init; }

    public required RecordingDialogs Dialogs { get; init; }

    public required RecordingDesktop Desktop { get; init; }

    public required FakeUpdateChannel Releases { get; init; }

    public required FakeSyncProvider Cloud { get; init; }

    public required InMemoryBackupFiles Files { get; init; }

    public required FakeSystemTheme SystemTheme { get; init; }

    public required ResourceDictionary Resources { get; init; }

    public static TestApp Create(
        Database? database = null,
        string version = "0.3.0",
        PublishedRelease? latest = null,
        ResourceDictionary? resources = null)
    {
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryDatabaseStore(database ?? SampleDatabase());
        var dialogs = new RecordingDialogs();
        var desktop = new RecordingDesktop();
        var releases = new FakeUpdateChannel { Latest = latest };
        var cloud = new FakeSyncProvider();
        var files = new InMemoryBackupFiles();
        var systemTheme = new FakeSystemTheme();
        resources ??= new ResourceDictionary();
        var adapters = new AppAdapters(time, store, new FixedRateProvider(), cloud, releases, releases, releases, files, systemTheme, new RecordingLog(), DataFolder);
        return new TestApp
        {
            Graph = new AppGraph(new BuildInfo(version), adapters, resources, dialogs, desktop),
            Time = time,
            Store = store,
            Dialogs = dialogs,
            Desktop = desktop,
            Releases = releases,
            Cloud = cloud,
            Files = files,
            SystemTheme = systemTheme,
            Resources = resources,
        };
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
