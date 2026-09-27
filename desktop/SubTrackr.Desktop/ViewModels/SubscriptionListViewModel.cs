using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SubTrackr.Core.Analytics;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The dashboard's subscription list: every live subscription, most expensive first, narrowed by
/// the search text and the category filter. The category choice survives a reload while that
/// category still exists.
/// </summary>
public sealed partial class SubscriptionListViewModel : ObservableObject
{
    public const string AllCategories = "All categories";

    private readonly IServiceLogoSource logos;
    private List<SubscriptionRowViewModel> allRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearchPlaceholder))]
    private string searchText = "";

    [ObservableProperty]
    private string selectedCategory = AllCategories;

    public SubscriptionListViewModel(IServiceLogoSource logos)
    {
        ArgumentNullException.ThrowIfNull(logos);
        this.logos = logos;
    }

    public ObservableCollection<SubscriptionRowViewModel> Subscriptions { get; } = [];

    public ObservableCollection<string> Categories { get; } = [];

    public bool ShowSearchPlaceholder => string.IsNullOrEmpty(SearchText);

    /// <summary>
    /// Replaces the rows with the summary's subscriptions, keeping the search and filter. With
    /// <paramref name="showServiceLogos"/> off no row can request a service logo.
    /// </summary>
    public void Load(SpendSummary summary, string baseCurrency, bool showServiceLogos)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var rowLogos = showServiceLogos ? logos : null;
        allRows = summary.PerSub
            .OrderByDescending(p => p.MonthlyBase)
            .Select(p => new SubscriptionRowViewModel(p, baseCurrency, rowLogos))
            .ToList();
        RebuildCategories();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    private void RebuildCategories()
    {
        var names = allRows.Select(r => r.Category).Distinct().Order(StringComparer.CurrentCulture).ToList();
        var current = SelectedCategory;
        Categories.Clear();
        Categories.Add(AllCategories);
        foreach (var name in names)
        {
            Categories.Add(name);
        }

        SelectedCategory = Categories.Contains(current) ? current : AllCategories;
    }

    private void ApplyFilter()
    {
        var query = (SearchText ?? "").Trim();
        IEnumerable<SubscriptionRowViewModel> rows = allRows;
        if (SelectedCategory != AllCategories)
        {
            rows = rows.Where(r => r.Category == SelectedCategory);
        }

        if (query.Length > 0)
        {
            rows = rows.Where(r =>
                r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Subscriptions.Clear();
        foreach (var row in rows)
        {
            Subscriptions.Add(row);
        }
    }
}
