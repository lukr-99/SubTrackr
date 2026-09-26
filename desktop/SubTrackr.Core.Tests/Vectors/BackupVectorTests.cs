using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Backup;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Vectors;

/// <summary>
/// Runs contracts/vectors/backup.json (the file Android runs too) through the real parser, the
/// restore, and the ledger's save.
/// </summary>
public class BackupVectorTests
{
    private static readonly string VectorPath = Path.Combine(AppContext.BaseDirectory, "vectors", "backup.json");

    private static readonly JsonParser Parser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

    private static readonly JsonFormatter Formatter = new(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));

    public static IEnumerable<object[]> Cases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(VectorPath));
        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return [testCase.GetProperty("id").GetString()!, testCase.GetRawText()];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Restore_MatchesVector(string id, string json)
    {
        using var document = JsonDocument.Parse(json);
        var testCase = document.RootElement;
        var local = Parser.Parse<Database>(testCase.GetProperty("local").GetRawText());
        var store = new InMemoryDatabaseStore(local);
        var ledger = SubscriptionLedger.Open(store, new FakeTimeProvider(Samples.Now));
        var before = store.Stored!;
        var service = new BackupService(ledger, new InMemoryBackupFiles(), new FakeTimeProvider(Samples.Now), new RecordingLog(), "0.3.0");
        var text = testCase.TryGetProperty("backupText", out var raw) ? raw.GetString()! : testCase.GetProperty("backup").GetRawText();
        var mode = testCase.GetProperty("mode").GetString() == "REPLACE" ? RestoreMode.Replace : RestoreMode.Merge;

        var report = service.Restore(text, mode);

        var expected = testCase.GetProperty("expected");
        if (!expected.GetProperty("ok").GetBoolean())
        {
            Assert.False(report.Succeeded, id);
            Assert.Equal(expected.GetProperty("error").GetString(), BackupFormat.Code(report.Error!.Value));
            Assert.Equal(before, store.Stored);
            Assert.Equal(before.Subscriptions, ledger.Subscriptions);
            return;
        }

        Assert.True(report.Succeeded, $"{id}: {report.Error} {report.Detail}");
        Assert.Equal(expected.GetProperty("added").GetInt32(), report.Added);
        Assert.Equal(expected.GetProperty("updated").GetInt32(), report.Updated);
        Assert.Equal(expected.GetProperty("unchanged").GetInt32(), report.Unchanged);
        Assert.Equal(expected.GetProperty("total").GetInt32(), report.Total);

        var saved = store.Stored!;
        var actual = saved.Subscriptions
            .OrderBy(s => s.Id, StringComparer.Ordinal)
            .Select(s => (s.Id, s.UpdatedAt, s.DeletedAt))
            .ToList();
        var wanted = expected.GetProperty("subscriptions").EnumerateArray()
            .Select(s => (s.GetProperty("id").GetString()!, s.GetProperty("updatedAt").GetString()!, s.GetProperty("deletedAt").GetString()!))
            .ToList();
        Assert.Equal(wanted, actual);

        if (expected.TryGetProperty("settings", out var settings))
        {
            using var savedSettings = JsonDocument.Parse(Formatter.Format(saved.Settings));
            foreach (var key in settings.EnumerateObject())
            {
                var value = savedSettings.RootElement.GetProperty(key.Name);
                if (key.Value.ValueKind == JsonValueKind.Number)
                {
                    Assert.Equal(key.Value.GetDouble(), value.GetDouble());
                }
                else
                {
                    Assert.Equal(key.Value.GetString(), value.GetString());
                }
            }
        }
    }
}
