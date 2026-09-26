using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>A store that keeps a copy in memory and can be told to fail its next saves.</summary>
public sealed class InMemoryDatabaseStore(Database? initial = null) : IDatabaseStore
{
    private Database? stored = initial?.Clone();

    public string Location => "memory";

    public int SaveCount { get; private set; }

    public bool FailSaves { get; set; }

    public Database? Stored => stored?.Clone();

    public Database? Load() => stored?.Clone();

    public void Save(Database database)
    {
        if (FailSaves)
        {
            throw new IOException("The disk is full.");
        }

        stored = database.Clone();
        SaveCount++;
    }
}
