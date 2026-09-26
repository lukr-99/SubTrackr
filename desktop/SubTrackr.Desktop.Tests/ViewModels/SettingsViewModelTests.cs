using System.Windows.Media;
using SubTrackr.Core.Contracts;
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
    }

    [Fact]
    public void SelectedTheme_Changed_SavesAndAppliesAtOnce()
    {
        using var app = TestApp.Create();
        var settings = app.Graph.Settings;
        Assert.Equal(ThemeMode.System, settings.SelectedTheme!.Mode);

        settings.SelectedTheme = settings.ThemeOptions.Single(o => o.Mode == ThemeMode.Dark);

        Assert.Equal(ThemeMode.Dark, app.Store.Stored!.Settings.ThemeMode);
        Assert.True(app.Graph.Theme.IsDark);
        Assert.Equal(app.Graph.Theme.Tokens.Dark.Neutral.Background, ((SolidColorBrush)app.Resources["Bg"]).Color);
    }

    [Fact]
    public void StoredTheme_IsAppliedAtStartup()
    {
        var database = TestApp.SampleDatabase();
        database.Settings.ThemeMode = ThemeMode.Light;
        using var app = TestApp.Create(database);

        Assert.Equal(ThemeMode.Light, app.Graph.Theme.Mode);
        Assert.Equal("Light", app.Graph.Settings.SelectedTheme!.Label);
    }

    [Fact]
    public void SystemTheme_WindowsTurnsDark_AppFollows()
    {
        using var app = TestApp.Create();

        app.SystemTheme.Switch(dark: true);

        Assert.True(app.Graph.Theme.IsDark);
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
    public void OpenDataFolderCommand_OpensTheDataFolder()
    {
        using var app = TestApp.Create();

        app.Graph.Settings.OpenDataFolderCommand.Execute(null);

        Assert.Equal([TestApp.DataFolder], app.Desktop.OpenedFolders);
    }
}
