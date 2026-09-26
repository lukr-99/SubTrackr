using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class SettingsViewModelTests
{
    [Fact]
    public void Load_ShowsStoredSettings()
    {
        using var app = TestApp.Create();
        var settings = app.Graph.Settings;

        Assert.Equal("CZK", settings.BaseCurrency);
        Assert.Equal("35", settings.ThresholdText);
        Assert.Equal("2000", settings.BudgetText);
        Assert.Equal("SubTrackr v0.3.0", settings.VersionText);
    }

    [Fact]
    public async Task SaveCommand_StoresValuesAndReturnsToDashboard()
    {
        using var app = TestApp.Create();
        app.Graph.Main.Open(AppPage.Settings);
        var settings = app.Graph.Settings;
        settings.BaseCurrency = "EUR";
        settings.ThresholdText = "2.5";
        settings.BudgetText = "120";

        await settings.SaveCommand.ExecuteAsync(null);

        var stored = app.Store.Stored!.Settings;
        Assert.Equal("EUR", stored.BaseCurrency);
        Assert.Equal(2.5, stored.WorthThreshold);
        Assert.Equal(120, stored.MonthlyBudget);
        Assert.True(app.Graph.Main.IsDashboardPage);
        Assert.Equal("EUR", app.Graph.Rates.Table.Anchor);
    }

    [Fact]
    public async Task SaveCommand_InvalidNumbers_KeepsStoredValues()
    {
        using var app = TestApp.Create();
        var settings = app.Graph.Settings;
        settings.BudgetText = "lots";

        await settings.SaveCommand.ExecuteAsync(null);

        Assert.Equal(2000, app.Store.Stored!.Settings.MonthlyBudget);
    }

    [Fact]
    public async Task SyncNowCommand_NoProject_AsksForOne()
    {
        using var app = TestApp.Create();

        await app.Graph.Settings.SyncNowCommand.ExecuteAsync(null);

        Assert.Equal("Enter the project URL and key first.", app.Graph.Settings.SyncStatus);
    }

    [Fact]
    public async Task SyncNowCommand_WithProject_ReportsCount()
    {
        using var app = TestApp.Create();
        var settings = app.Graph.Settings;
        settings.SyncUrl = "https://project.example";
        settings.SyncKey = "publishable-key";

        await settings.SyncNowCommand.ExecuteAsync(null);

        Assert.Equal("Synced · 8 items · 10:00", settings.SyncStatus);
        Assert.Equal(8, app.Cloud.Remote.Count);
    }

    [Fact]
    public void OpenDataFolderCommand_OpensTheDataFolder()
    {
        using var app = TestApp.Create();

        app.Graph.Settings.OpenDataFolderCommand.Execute(null);

        Assert.Equal([@"C:\Users\you\AppData\Roaming\SubTrackr"], app.Desktop.OpenedFolders);
    }
}
