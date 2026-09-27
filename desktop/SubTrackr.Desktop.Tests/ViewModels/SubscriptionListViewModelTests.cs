using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class SubscriptionListViewModelTests
{
    private readonly RecordingServiceLogos logos = new();

    [Fact]
    public void Load_SortsByMonthlyEquivalentInBaseCurrency()
    {
        var list = new SubscriptionListViewModel(logos);

        list.Load(Spend.Summarize(Spend.Monthly("Cheap", 100), Spend.Monthly("Euro", 10, "EUR"), Spend.Monthly("Dear", 300)), "CZK", true);

        Assert.Equal(["Dear", "Euro", "Cheap"], list.Subscriptions.Select(r => r.Name));
    }

    [Fact]
    public void Load_ListsEachCategoryOnceAfterAll()
    {
        var list = new SubscriptionListViewModel(logos);

        list.Load(Spend.Summarize(Spend.Monthly("A", 1, category: "Music"), Spend.Monthly("B", 2, category: "Food"), Spend.Monthly("C", 3, category: "Music")), "CZK", true);

        Assert.Equal([SubscriptionListViewModel.AllCategories, "Food", "Music"], list.Categories);
    }

    [Fact]
    public void SelectedCategory_AndSearch_NarrowTogether()
    {
        var list = new SubscriptionListViewModel(logos);
        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Tidal", 2, category: "Music"), Spend.Monthly("Spotify Kids", 3, category: "Family")), "CZK", true);

        list.SelectedCategory = "Music";
        list.SearchText = "  spot ";

        Assert.Equal(["Spotify"], list.Subscriptions.Select(r => r.Name));
    }

    [Fact]
    public void SearchText_MatchesTheCategoryToo()
    {
        var list = new SubscriptionListViewModel(logos);
        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Netflix", 2, category: "Video")), "CZK", true);

        list.SearchText = "VIDEO";

        Assert.Equal(["Netflix"], list.Subscriptions.Select(r => r.Name));
        Assert.False(list.ShowSearchPlaceholder);
    }

    [Fact]
    public void Load_KeepsTheFilterWhileItsCategoryExists_ElseFallsBackToAll()
    {
        var list = new SubscriptionListViewModel(logos);
        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Netflix", 2, category: "Video")), "CZK", true);
        list.SelectedCategory = "Music";

        list.Load(Spend.Summarize(Spend.Monthly("Spotify", 1, category: "Music"), Spend.Monthly("Tidal", 2, category: "Music")), "CZK", true);
        Assert.Equal("Music", list.SelectedCategory);
        Assert.Equal(2, list.Subscriptions.Count);

        list.Load(Spend.Summarize(Spend.Monthly("Netflix", 2, category: "Video")), "CZK", true);
        Assert.Equal(SubscriptionListViewModel.AllCategories, list.SelectedCategory);
        Assert.Equal(["Netflix"], list.Subscriptions.Select(r => r.Name));
    }

    [Fact]
    public void Load_LogosShown_RequestsEachWebsitesLogoOnceWhenAskedFor()
    {
        var list = new SubscriptionListViewModel(logos);
        list.Load(Spend.Summarize(WithWebsite("Netflix", " netflix.com "), Spend.Monthly("Gym", 1)), "CZK", true);
        var netflix = list.Subscriptions.Single(r => r.Name == "Netflix");
        var gym = list.Subscriptions.Single(r => r.Name == "Gym");
        Assert.Empty(logos.Requested);

        Assert.True(netflix.HasLogo);
        Assert.NotNull(netflix.Logo);
        Assert.Same(netflix.Logo, netflix.Logo);
        Assert.False(gym.HasLogo);
        Assert.Null(gym.Logo);

        Assert.Equal([" netflix.com "], logos.Requested);
    }

    [Fact]
    public void Load_LogosHidden_NeverRequestsALogo()
    {
        var list = new SubscriptionListViewModel(logos);

        list.Load(Spend.Summarize(WithWebsite("Netflix", "netflix.com")), "CZK", false);
        var row = Assert.Single(list.Subscriptions);

        Assert.False(row.HasLogo);
        Assert.Null(row.Logo);
        Assert.Empty(logos.Requested);
    }

    private static Core.Contracts.Subscription WithWebsite(string name, string website)
    {
        var subscription = Spend.Monthly(name, 100);
        subscription.Website = website;
        return subscription;
    }
}
