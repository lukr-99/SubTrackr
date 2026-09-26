using Google.Protobuf;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;

namespace SubTrackr.Infrastructure.Storage;

/// <summary>
/// Keeps the whole <see cref="Database"/> in one JSON file (the protobuf JSON mapping). Saves
/// write a temporary file next to it and swap it in, so a crash mid-save keeps the old file.
/// Loading ignores fields this build does not know, so a file a newer build wrote still opens.
/// </summary>
public sealed class JsonDatabaseStore : IDatabaseStore
{
    private static readonly JsonFormatter Formatter =
        new(JsonFormatter.Settings.Default.WithIndentation("  "));

    private static readonly JsonParser Parser =
        new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

    private readonly string filePath;

    public JsonDatabaseStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = filePath;
    }

    public string Location => filePath;

    public Database? Load() =>
        File.Exists(filePath) ? Parser.Parse<Database>(File.ReadAllText(filePath)) : null;

    public void Save(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        AtomicFile.WriteAllText(filePath, Formatter.Format(database));
    }
}
