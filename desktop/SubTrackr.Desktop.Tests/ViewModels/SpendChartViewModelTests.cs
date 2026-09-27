using Microsoft.Extensions.Time.Testing;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class SpendChartViewModelTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Update_Donut_OneSlicePerCategoryLargestFirst()
    {
        var chart = new SpendChartViewModel(time);

        chart.Update(Spend.Summarize(Spend.Monthly("A", 100, category: "Music"), Spend.Monthly("B", 300, category: "Video"), Spend.Monthly("C", 50, category: "Music")), "CZK");

        Assert.Equal(["Video", "Music"], chart.ChartSlices.Select(s => s.Label));
        Assert.Equal([300d, 150d], chart.ChartSlices.Select(s => s.Value));
        Assert.Equal([0, 1], chart.ChartSlices.Select(s => s.ColorIndex));
    }

    [Fact]
    public void Update_MoreCategoriesThanColors_RollsTheRestIntoOther()
    {
        var chart = new SpendChartViewModel(time);
        var subscriptions = Enumerable.Range(1, 9).Select(i => Spend.Monthly("S" + i, 100 - i, category: "C" + i)).ToArray();

        chart.Update(Spend.Summarize(subscriptions), "CZK");

        Assert.Equal(SpendChartViewModel.MaxCategorySlices + 1, chart.ChartSlices.Count);
        var other = chart.ChartSlices[^1];
        Assert.Equal("Other", other.Label);
        Assert.Equal(92d + 91d, other.Value);
        Assert.Equal(SpendChartViewModel.MaxCategorySlices, other.ColorIndex);
    }

    [Fact]
    public void SetChart_Trend_AddsUpTwelveMonthsFromThisMonth()
    {
        var chart = new SpendChartViewModel(time);
        chart.Update(Spend.Summarize(Spend.Monthly("A", 100)), "CZK");

        chart.SetChartCommand.Execute(ChartType.Trend);

        Assert.Equal(12, chart.ChartSlices.Count);
        Assert.Equal("Nov", chart.ChartSlices[0].Label);
        Assert.Equal("Oct", chart.ChartSlices[11].Label);
        Assert.Equal(1200d, chart.ChartSlices[11].Value);
    }

    [Fact]
    public void SetChart_BeforeAnySummary_ShowsNothing()
    {
        var chart = new SpendChartViewModel(time);

        chart.SetChartCommand.Execute(ChartType.Bars);

        Assert.Empty(chart.ChartSlices);
    }
}
