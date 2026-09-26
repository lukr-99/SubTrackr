using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class DashboardViewModelTests
{
    [Fact]
    public void Refresh_SampleData_ShowsTotalsInBaseCurrency()
    {
        using var app = TestApp.Create();
        var dashboard = app.Graph.Dashboard;

        Assert.Equal("CZK", dashboard.BaseCurrency);
        Assert.Equal(7, dashboard.ActiveCount);
        Assert.Equal(8, dashboard.Subscriptions.Count);
        Assert.EndsWith(" Kč", dashboard.MonthlyText, StringComparison.Ordinal);
        Assert.True(dashboard.HasBudget);
    }

    [Fact]
    public void Alerts_TrialEndingSoon_IsListed()
    {
        using var app = TestApp.Create();

        Assert.Contains(app.Graph.Dashboard.Alerts, alert => alert.Title == "Claude trial ends" && alert.Detail.StartsWith("in 2 days", StringComparison.Ordinal));
    }

    [Fact]
    public void SearchText_FiltersByNameOrCategory()
    {
        using var app = TestApp.Create();
        var dashboard = app.Graph.Dashboard;

        dashboard.SearchText = "transport";

        Assert.Equal(["City transit pass", "Ride pass"], dashboard.Subscriptions.Select(r => r.Name).Order());
    }

    [Fact]
    public void AddCommand_SavesWhatTheFormReturns()
    {
        using var app = TestApp.Create();
        app.Dialogs.EditAnswer = editor =>
        {
            editor.Name = "Newspaper";
            editor.Amount = "4.50";
            editor.Currency = "EUR";
            return editor.TryBuild();
        };

        app.Graph.Dashboard.AddCommand.Execute(null);

        Assert.Contains(app.Store.Stored!.Subscriptions, s => s.Name == "Newspaper" && s.Cost.MinorUnits == 450);
        Assert.Contains(app.Graph.Dashboard.Subscriptions, r => r.Name == "Newspaper");
    }

    [Fact]
    public void DeleteCommand_Declined_KeepsTheSubscription()
    {
        using var app = TestApp.Create();
        app.Dialogs.ConfirmAnswer = false;
        var row = app.Graph.Dashboard.Subscriptions[0];

        app.Graph.Dashboard.DeleteCommand.Execute(row);

        Assert.Single(app.Dialogs.Questions);
        Assert.Contains(app.Graph.Dashboard.Subscriptions, r => r.Name == row.Name);
    }

    [Fact]
    public void DeleteCommand_Confirmed_LeavesTombstone()
    {
        using var app = TestApp.Create();
        var row = app.Graph.Dashboard.Subscriptions[0];

        app.Graph.Dashboard.DeleteCommand.Execute(row);

        Assert.DoesNotContain(app.Graph.Dashboard.Subscriptions, r => r.Name == row.Name);
        Assert.NotEmpty(app.Store.Stored!.Subscriptions.Single(s => s.Id == row.Model.Id).DeletedAt);
    }

    [Fact]
    public void BaseCurrency_Changed_SavesAndReanchors()
    {
        using var app = TestApp.Create();

        app.Graph.Dashboard.BaseCurrency = "EUR";

        Assert.Equal("EUR", app.Store.Stored!.Settings.BaseCurrency);
        Assert.Equal("EUR", app.Graph.Rates.Table.Anchor);
        Assert.StartsWith("€", app.Graph.Dashboard.MonthlyText, StringComparison.Ordinal);
    }

    [Fact]
    public void SetChartCommand_Trend_ShowsTwelveMonths()
    {
        using var app = TestApp.Create();

        app.Graph.Dashboard.SetChartCommand.Execute(ChartType.Trend);

        Assert.Equal(12, app.Graph.Dashboard.ChartSlices.Count);
        Assert.Equal("Sep", app.Graph.Dashboard.ChartSlices[0].Label);
    }

    [Fact]
    public void PausedSubscription_IsListedButNotCounted()
    {
        using var app = TestApp.Create();

        var paused = app.Graph.Dashboard.Subscriptions.Single(r => r.IsPaused);

        Assert.Equal(SubStatus.Paused, paused.Model.Status);
        Assert.Equal(7, app.Graph.Dashboard.ActiveCount);
    }
}
