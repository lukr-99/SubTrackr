using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Analytics;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The dashboard chart: which kind shows, and its slices. Donut and bars split active spend by
/// category, rolling everything past the palette into "Other"; the trend adds the monthly total up
/// over the next twelve months.
/// </summary>
public sealed partial class SpendChartViewModel : ObservableObject
{
    /// <summary>Named categories before the rest roll into "Other"; the chart palette has one more color.</summary>
    public const int MaxCategorySlices = 7;

    private readonly TimeProvider time;
    private SpendSummary? summary;
    private string baseCurrency = "";

    public SpendChartViewModel(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        this.time = time;
    }

    [ObservableProperty]
    private ChartType chartType = ChartType.Donut;

    public ObservableCollection<ChartSlice> ChartSlices { get; } = [];

    /// <summary>Rebuilds the slices from the summary; the trend starts in the current month.</summary>
    public void Update(SpendSummary summary, string baseCurrency)
    {
        ArgumentNullException.ThrowIfNull(summary);
        this.summary = summary;
        this.baseCurrency = baseCurrency;
        Build();
    }

    [RelayCommand]
    private void SetChart(ChartType type) => ChartType = type;

    partial void OnChartTypeChanged(ChartType value) => Build();

    private void Build()
    {
        ChartSlices.Clear();
        if (summary is null)
        {
            return;
        }

        if (ChartType == ChartType.Trend)
        {
            BuildTrend(summary);
        }
        else
        {
            BuildCategories(summary);
        }
    }

    // Cumulative spend over the next 12 months (base currency).
    private void BuildTrend(SpendSummary summary)
    {
        var monthly = (double)summary.MonthlyBase;
        var month = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        double cumulative = 0;
        for (var i = 0; i < 12; i++)
        {
            cumulative += monthly;
            var label = month.AddMonths(i).ToString("MMM", CultureInfo.InvariantCulture);
            ChartSlices.Add(new ChartSlice { Label = label, Value = cumulative, ColorIndex = 0 });
        }
    }

    private void BuildCategories(SpendSummary summary)
    {
        var categories = summary.ByCategory.Where(c => c.MonthlyBase > 0).ToList();
        for (var i = 0; i < categories.Count && i < MaxCategorySlices; i++)
        {
            ChartSlices.Add(new ChartSlice
            {
                Label = categories[i].Category,
                Value = (double)categories[i].MonthlyBase,
                ColorIndex = i,
                ValueLabel = Formatting.Money(categories[i].MonthlyBase, baseCurrency),
            });
        }

        if (categories.Count > MaxCategorySlices)
        {
            var rest = categories.Skip(MaxCategorySlices).Sum(c => c.MonthlyBase);
            ChartSlices.Add(new ChartSlice
            {
                Label = "Other",
                Value = (double)rest,
                ColorIndex = MaxCategorySlices,
                ValueLabel = Formatting.Money(rest, baseCurrency),
            });
        }
    }
}
