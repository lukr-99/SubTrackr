using Google.Protobuf;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;

namespace SubTrackr.Infrastructure.Storage;

/// <summary>
/// Keeps the whole <see cref="Database"/> in one JSON file (the protobuf JSON mapping). Saves
/// write a temporary file next to it and swap it in, so a crash mid-save keeps the old file.
/// Loading ignores fields this build does not know, so a file a newer build wrote still opens.
/// A file that cannot be parsed is renamed to <c>data.json.unreadable-&lt;UTC time&gt;</c> and never
/// overwritten, so the app can start fresh without destroying data someone may still recover.
/// </summary>
public sealed class JsonDatabaseStore : IDatabaseStore
{
    private static readonly JsonFormatter Formatter =
        new(JsonFormatter.Settings.Default.WithIndentation("  "));

    private static readonly JsonParser Parser =
        new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

    private readonly string filePath;
    private readonly TimeProvider time;

    public JsonDatabaseStore(string filePath, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = filePath;
        this.time = time ?? TimeProvider.System;
    }

    public string Location => filePath;

    /// <summary>Where the last unreadable file was moved, or null when every load succeeded.</summary>
    public string? SetAsidePath { get; private set; }

    public Database? Load()
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            return Parser.Parse<Database>(File.ReadAllText(filePath));
        }
        catch (InvalidProtocolBufferException)
        {
            SetAside();
            return null;
        }
        catch (InvalidJsonException)
        {
            SetAside();
            return null;
        }
    }

    private void SetAside()
    {
        var stamp = time.GetUtcNow().ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
        var target = $"{filePath}.unreadable-{stamp}";
        File.Move(filePath, target, overwrite: false);
        SetAsidePath = target;
    }

    public void Save(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        AtomicFile.WriteAllText(filePath, Formatter.Format(database));
    }
}
