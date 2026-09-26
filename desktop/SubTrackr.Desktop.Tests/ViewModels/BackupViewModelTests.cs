using SubTrackr.Desktop.Tests.Hosting;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class BackupViewModelTests
{
    private const string BackupPath = @"C:\Users\you\Documents\SubTrackr-backup.json";

    [Fact]
    public void BackUpCommand_SuggestsTimestampedNameAndWritesTheFile()
    {
        using var app = TestApp.Create();
        app.Dialogs.SavePath = BackupPath;

        app.Graph.Settings.Backup.BackUpCommand.Execute(null);

        Assert.Equal(["SubTrackr-backup-20260926-100000.json"], app.Dialogs.SuggestedNames);
        Assert.Contains("\"format\": \"subtrackr-backup\"", app.Files.Files[BackupPath], StringComparison.Ordinal);
        Assert.Equal("Saved SubTrackr-backup.json.", app.Graph.Settings.Backup.ResultText);
        Assert.False(app.Graph.Settings.Backup.IsError);
    }

    [Fact]
    public void BackUpCommand_Cancelled_WritesNothing()
    {
        using var app = TestApp.Create();

        app.Graph.Settings.Backup.BackUpCommand.Execute(null);

        Assert.Empty(app.Files.Files);
        Assert.Equal("", app.Graph.Settings.Backup.ResultText);
    }

    [Fact]
    public void RestoreCommand_MergeByDefault_ReportsCounts()
    {
        using var app = TestApp.Create();
        app.Files.Files[BackupPath] = app.Graph.Backups.CreateBackup();
        app.Dialogs.OpenPath = BackupPath;

        app.Graph.Settings.Backup.RestoreCommand.Execute(null);

        Assert.True(app.Graph.Settings.Backup.IsMergeMode);
        Assert.Empty(app.Dialogs.Questions);
        Assert.Equal("Restored: 0 added, 0 updated, 8 unchanged. 8 subscriptions in total.", app.Graph.Settings.Backup.ResultText);
    }

    [Fact]
    public void RestoreCommand_ReplaceDeclined_ChangesNothing()
    {
        using var app = TestApp.Create();
        app.Files.Files[BackupPath] = "{}";
        app.Dialogs.OpenPath = BackupPath;
        app.Dialogs.ConfirmAnswer = false;
        app.Graph.Settings.Backup.IsReplaceMode = true;

        app.Graph.Settings.Backup.RestoreCommand.Execute(null);

        Assert.Single(app.Dialogs.Questions);
        Assert.Equal("", app.Graph.Settings.Backup.ResultText);
    }

    [Fact]
    public void RestoreCommand_Replace_SwapsTheDataAndRefreshesTheDashboard()
    {
        using var app = TestApp.Create();
        var backup = app.Graph.Backups.CreateBackup();
        app.Graph.Dashboard.DeleteCommand.Execute(app.Graph.Dashboard.Subscriptions[0]);
        app.Graph.Dashboard.BaseCurrency = "EUR";
        app.Files.Files[BackupPath] = backup;
        app.Dialogs.OpenPath = BackupPath;
        app.Graph.Settings.Backup.IsReplaceMode = true;

        app.Graph.Settings.Backup.RestoreCommand.Execute(null);

        Assert.Equal("Restored: 8 added, 0 updated, 0 unchanged. 8 subscriptions in total.", app.Graph.Settings.Backup.ResultText);
        Assert.Equal(8, app.Graph.Dashboard.Subscriptions.Count);
        Assert.Equal("CZK", app.Graph.Dashboard.BaseCurrency);
        Assert.Equal("CZK", app.Graph.Settings.BaseCurrency);
        Assert.Equal("CZK", app.Graph.Rates.Table.Anchor);
    }

    [Fact]
    public void RestoreCommand_NotABackup_ExplainsInPlainWords()
    {
        using var app = TestApp.Create();
        app.Files.Files[BackupPath] = """{"format":"something-else","formatVersion":1,"database":{}}""";
        app.Dialogs.OpenPath = BackupPath;

        app.Graph.Settings.Backup.RestoreCommand.Execute(null);

        Assert.Equal("Nothing changed: this file isn't a SubTrackr backup.", app.Graph.Settings.Backup.ResultText);
        Assert.True(app.Graph.Settings.Backup.IsError);
    }
}
