using Google.Protobuf;
using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Storage;

/// <summary>
/// Loads/saves the entire <see cref="Database"/> as a single JSON file (the protobuf
/// JSON encoding). One file = the whole app state, which is also exactly what we will
/// sync later (SPEC.md). Writes are atomic (temp + replace).
/// </summary>
public sealed class DataStore
{
    public const string CurrentSchemaVersion = "0.1";

    private static readonly JsonFormatter Formatter =
        new(JsonFormatter.Settings.Default.WithIndentation("  "));

    public string FilePath { get; }

    public DataStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultPath();
    }

    public static string DefaultPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubTrackr");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "data.json");
    }

    /// <summary>Load the database, or seed a fresh one on first run.</summary>
    public Database Load()
    {
        if (!File.Exists(FilePath))
        {
            var seeded = SeedData.CreateInitialDatabase();
            Save(seeded);
            return seeded;
        }

        var json = File.ReadAllText(FilePath);
        var db = JsonParser.Default.Parse<Database>(json);
        if (string.IsNullOrEmpty(db.SchemaVersion))
            db.SchemaVersion = CurrentSchemaVersion;
        return db;
    }

    public void Save(Database db)
    {
        db.SchemaVersion = CurrentSchemaVersion;
        var json = Formatter.Format(db);

        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(FilePath))
            File.Replace(tmp, FilePath, null);
        else
            File.Move(tmp, FilePath);
    }
}
