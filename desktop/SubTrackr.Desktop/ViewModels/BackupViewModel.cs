using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Backup;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The backup card: save a backup file, or restore one by merging (the default) or replacing, which
/// asks first. The result line says what happened in plain words.
/// </summary>
public sealed partial class BackupViewModel : ObservableObject
{
    private readonly BackupService backups;
    private readonly IDialogService dialogs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReplaceMode))]
    private bool isMergeMode = true;

    [ObservableProperty]
    private string resultText = "";

    [ObservableProperty]
    private bool hasError;

    public BackupViewModel(BackupService backups, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(backups);
        ArgumentNullException.ThrowIfNull(dialogs);
        this.backups = backups;
        this.dialogs = dialogs;
    }

    public bool IsReplaceMode
    {
        get => !IsMergeMode;
        set => IsMergeMode = !value;
    }

    [RelayCommand]
    private void BackUp()
    {
        var path = dialogs.PickSaveFile(backups.SuggestedFileName());
        if (path is null)
        {
            return;
        }

        var error = backups.Export(path);
        Show(error is null ? $"Saved {Path.GetFileName(path)}." : $"The backup wasn't saved: {error}", error is not null);
    }

    [RelayCommand]
    private void Restore()
    {
        var path = dialogs.PickOpenFile();
        if (path is null)
        {
            return;
        }

        var mode = IsMergeMode ? RestoreMode.Merge : RestoreMode.Replace;
        if (mode == RestoreMode.Replace && !dialogs.Confirm(
            "Replace all subscriptions and settings on this device with the backup?\n\n" +
            "Anything added since the backup was made is lost here. Your sync project stays, and the next sync still merges with the cloud.",
            "Replace with backup"))
        {
            return;
        }

        var report = backups.RestoreFrom(path, mode);
        Show(report.Succeeded ? Describe(report) : Explain(report), !report.Succeeded);
    }

    private void Show(string text, bool error)
    {
        ResultText = text;
        HasError = error;
    }

    private static string Describe(RestoreReport report) =>
        $"Restored: {report.Added} added, {report.Updated} updated, {report.Unchanged} unchanged. " +
        $"{report.Total} {(report.Total == 1 ? "subscription" : "subscriptions")} in total.";

    private static string Explain(RestoreReport report) => report.Error switch
    {
        BackupError.InvalidJson => "Nothing changed: this file isn't a readable backup (not JSON, or larger than 10 MB).",
        BackupError.UnsupportedFormat => "Nothing changed: this file isn't a SubTrackr backup.",
        BackupError.UnsupportedVersion => "Nothing changed: this backup comes from a newer or unknown version of SubTrackr.",
        BackupError.InvalidRecord => $"Nothing changed: the backup has a damaged entry ({report.Detail}).",
        _ => $"Nothing changed: {report.Detail}",
    };
}
