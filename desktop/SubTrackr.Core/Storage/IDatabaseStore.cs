using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Storage;

/// <summary>
/// Keeps the whole <see cref="Database"/> somewhere durable. <see cref="Save"/> is atomic: when it
/// throws, whatever was stored before is still there, unchanged.
/// </summary>
public interface IDatabaseStore
{
    /// <summary>Where the data lives, for display (a file path for the real store).</summary>
    string Location { get; }

    /// <summary>The stored database, or null when nothing has been saved yet.</summary>
    Database? Load();

    void Save(Database database);
}
