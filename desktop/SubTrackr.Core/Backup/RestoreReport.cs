namespace SubTrackr.Core.Backup;

/// <summary>
/// What a restore did: the counts when it succeeded, otherwise the validation error (or null for a
/// file that could not be read or saved) and a short detail.
/// </summary>
public sealed record RestoreReport(bool Succeeded, BackupError? Error, string Detail, int Added, int Updated, int Unchanged, int Total)
{
    public static RestoreReport Done(RestoreOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return new RestoreReport(true, null, "", outcome.Added, outcome.Updated, outcome.Unchanged, outcome.Total);
    }

    public static RestoreReport Refused(BackupError error, string detail) => new(false, error, detail, 0, 0, 0, 0);

    public static RestoreReport Failed(string detail) => new(false, null, detail, 0, 0, 0, 0);
}
