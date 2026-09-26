namespace SubTrackr.Core.Backup;

/// <summary>Why a backup was refused (SPEC.md section 9.2). Validation stops at the first one.</summary>
public enum BackupError
{
    /// <summary><c>INVALID_JSON</c>: not a JSON object, or larger than 10 MB.</summary>
    InvalidJson,

    /// <summary><c>UNSUPPORTED_FORMAT</c>: <c>format</c> is not <c>subtrackr-backup</c>.</summary>
    UnsupportedFormat,

    /// <summary><c>UNSUPPORTED_VERSION</c>: <c>formatVersion</c> is missing, not an integer, or not 1.</summary>
    UnsupportedVersion,

    /// <summary><c>INVALID_RECORD</c>: no <c>database</c>, or a subscription breaks a rule.</summary>
    InvalidRecord,
}
