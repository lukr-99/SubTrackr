using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Backup;

/// <summary>A validated backup database, or the first reason it was refused.</summary>
public sealed record BackupReadResult(Database? Database, BackupError? Error, string Detail)
{
    public bool IsValid => Database is not null;

    public static BackupReadResult Valid(Database database) => new(database, null, "");

    public static BackupReadResult Invalid(BackupError error, string detail) => new(null, error, detail);
}
