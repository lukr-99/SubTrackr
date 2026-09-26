using System.Globalization;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;

namespace SubTrackr.Core.Backup;

/// <summary>
/// The backup use cases: write the whole ledger to a file, and restore one by merging or replacing.
/// A restore validates first and builds the new database in memory; only then does the ledger's
/// atomic save run, so any failure leaves the device's data as it was.
/// </summary>
public sealed class BackupService
{
    private readonly SubscriptionLedger ledger;
    private readonly IBackupFiles files;
    private readonly TimeProvider time;
    private readonly IAppLog log;
    private readonly string appVersion;

    public BackupService(SubscriptionLedger ledger, IBackupFiles files, TimeProvider time, IAppLog log, string appVersion)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(log);
        this.ledger = ledger;
        this.files = files;
        this.time = time;
        this.log = log;
        this.appVersion = appVersion;
    }

    /// <summary><c>SubTrackr-backup-YYYYMMDD-HHMMSS.json</c> in local time.</summary>
    public string SuggestedFileName() =>
        "SubTrackr-backup-" + time.GetLocalNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json";

    /// <summary>The backup text for the current data.</summary>
    public string CreateBackup() => BackupWriter.Write(ledger.Snapshot(), time.GetUtcNow(), appVersion);

    /// <summary>Writes a backup to <paramref name="path"/>; null on success, otherwise why it failed.</summary>
    public string? Export(string path)
    {
        try
        {
            files.WriteText(path, CreateBackup());
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            log.Error("Backup could not be written", exception);
            return exception.Message;
        }
    }

    /// <summary>Reads, validates, and restores the backup file at <paramref name="path"/>.</summary>
    public RestoreReport RestoreFrom(string path, RestoreMode mode)
    {
        string? text;
        try
        {
            text = files.ReadText(path, BackupFormat.MaxBytes);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            log.Error("Backup could not be read", exception);
            return RestoreReport.Failed(exception.Message);
        }

        return text is null
            ? RestoreReport.Refused(BackupError.InvalidJson, "larger than 10 MB")
            : Restore(text, mode);
    }

    /// <summary>Validates and restores backup text.</summary>
    public RestoreReport Restore(string text, RestoreMode mode)
    {
        var read = BackupReader.Read(text);
        if (!read.IsValid)
        {
            return RestoreReport.Refused(read.Error!.Value, read.Detail);
        }

        var outcome = BackupRestorer.Restore(ledger.Snapshot(), read.Database!, mode);
        try
        {
            ledger.Replace(outcome.Result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            log.Error("Restored data could not be saved", exception);
            return RestoreReport.Failed(exception.Message);
        }

        log.Info($"Restored a backup ({mode}): {outcome.Added} added, {outcome.Updated} updated, {outcome.Unchanged} unchanged.");
        return RestoreReport.Done(outcome);
    }
}
