namespace SubTrackr.Core.Backup;

/// <summary>The fixed facts of the backup file (SPEC.md section 9.1).</summary>
public static class BackupFormat
{
    public const string Name = "subtrackr-backup";

    public const int Version = 1;

    /// <summary>Anything larger is refused as <see cref="BackupError.InvalidJson"/>.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>The <c>platform</c> this app writes.</summary>
    public const string Platform = "desktop";

    /// <summary>The spec's code for an error, as the vectors and the Android app spell it.</summary>
    public static string Code(BackupError error) => error switch
    {
        BackupError.InvalidJson => "INVALID_JSON",
        BackupError.UnsupportedFormat => "UNSUPPORTED_FORMAT",
        BackupError.UnsupportedVersion => "UNSUPPORTED_VERSION",
        _ => "INVALID_RECORD",
    };
}
