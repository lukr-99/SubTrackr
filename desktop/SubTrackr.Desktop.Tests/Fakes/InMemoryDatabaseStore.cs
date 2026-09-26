using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>A store that keeps a copy in memory.</summary>
public sealed class InMemoryDatabaseStore(Database? initial = null) : IDatabaseStore
{
    private Database? stored = initial?.Clone();

    public string Location => @"C:\Users\you\AppData\Roaming\SubTrackr\data.json";

    public Database? Stored => stored?.Clone();

    public Database? Load() => stored?.Clone();

    public void Save(Database database) => stored = database.Clone();
}
