using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class SubscriptionListViewModelTests
{
    [Fact]
    public void Load_SortsByMonthlyEquivalentInBaseCurrency()
    {
        var list = new SubscriptionListViewModel();

        list.Load(Spend.Summarize(Spend.Monthly("Cheap", 100), Spend.Monthly("Euro", 10, "EUR"), Spend.Monthly("Dear", 300)), "CZK");

        Assert.Equal(["Dear", "Euro", "Cheap"], list.Subscriptions.Select(r => r.Name));
    }

    [Fact]
    public void Load_ListsEachCategoryOnceAfterAll()
    {
        var list = new SubscriptionListViewModel();

        list.Load(Spend.Summarize(Spend.Monthly("A", 1, category: "Music"), Spend.Monthly("B", 2, category: "Food"), Spend.Monthly("C", 3, category: "Music")), "CZK");

        Assert.Equal([SubscriptionListViewModel.AllCategories, "Food", "Music"], list.Categories);
    }

    [Fact]
    public void SelectedCategory_AndSearch_NarrowTogether()
    {
        var list = new SubscriptionListViewModel();
        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Tidal", 2, category: "Music"), Spend.Monthly("Spotify Kids", 3, category: "Family")), "CZK");

        list.SelectedCategory = "Music";
        list.SearchText = "  spot ";

        Assert.Equal(["Spotify"], list.Subscriptions.Select(r => r.Name));
    }

    [Fact]
    public void SearchText_MatchesTheCategoryToo()
    {
        var list = new SubscriptionListViewModel();
        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Netflix", 2, category: "Video")), "CZK");

        list.SearchText = "VIDEO";

        Assert.Equal(["Netflix"], list.Subscriptions.Select(r => r.Name));
        Assert.False(list.ShowSearchPlaceholder);
    }

    [Fact]
    public void Load_KeepsTheFilterWhileItsCategoryExists_ElseFallsBackToAll()
    {
        var list = new SubscriptionListViewModel();
        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Netflix", 2, category: "Video")), "CZK");
        list.SelectedCategory = "Music";

        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Tidal", 2, category: "Music")), "CZK");
        Assert.Equal("Music", list.SelectedCategory);
        Assert.Equal(2, list.Subscriptions.Count);

        list.Load(Spend.Summarize(Spend.Monthly("Netflix", 2, category: "Video")), "CZK");
        Assert.Equal(SubscriptionListViewModel.AllCategories, list.SelectedCategory);
        Assert.Equal(["Netflix"], list.Subscriptions.Select(r => r.Name));
    }
}
