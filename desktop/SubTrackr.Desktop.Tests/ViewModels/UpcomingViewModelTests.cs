using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class UpcomingViewModelTests
{
    private static readonly DateOnly Today = new(2026, 9, 26);

    [Fact]
    public void Update_TrialEndingWithinAWeek_Alerts()
    {
        var trial = Renewing("Trial", "2026-12-01");
        trial.TrialEnd = "2026-09-28";
        var later = Renewing("Later", "2026-12-01");
        later.TrialEnd = "2026-10-04";
        var upcoming = new UpcomingViewModel();

        upcoming.Update(Spend.Summarize(trial, later), Today);

        var alert = Assert.Single(upcoming.Alerts);
        Assert.Equal("Trial trial ends", alert.Title);
        Assert.StartsWith("in 2 days · then ", alert.Detail, StringComparison.Ordinal);
        Assert.True(alert.Urgent);
        Assert.True(upcoming.HasAlerts);
    }

    [Fact]
    public void Update_RenewalWithinThreeDays_Alerts()
    {
        var upcoming = new UpcomingViewModel();

        upcoming.Update(Spend.Summarize(Renewing("Soon", "2026-09-27"), Renewing("Later", "2026-09-30")), Today);

        var alert = Assert.Single(upcoming.Alerts);
        Assert.Equal("Soon renews", alert.Title);
        Assert.StartsWith("tomorrow · ", alert.Detail, StringComparison.Ordinal);
        Assert.Equal("🔔", alert.Icon);
    }

    [Fact]
    public void Update_PausedSubscription_NeverShows()
    {
        var paused = Renewing("Paused", "2026-09-26");
        paused.Status = SubStatus.Paused;
        var upcoming = new UpcomingViewModel();

        upcoming.Update(Spend.Summarize(paused), Today);

        Assert.Empty(upcoming.Alerts);
        Assert.Empty(upcoming.UpcomingRenewals);
        Assert.False(upcoming.HasAlerts);
    }

    [Fact]
    public void Update_ListsTheNextFiveRenewalsSoonestFirst()
    {
        var upcoming = new UpcomingViewModel();
        var subscriptions = Enumerable.Range(1, 7).Reverse().Select(i => Renewing("S" + i, $"2026-10-{i:00}")).ToArray();

        upcoming.Update(Spend.Summarize([.. subscriptions, Renewing("Undated", "")]), Today);

        Assert.Equal(["S1", "S2", "S3", "S4", "S5"], upcoming.UpcomingRenewals.Select(r => r.Name));
        Assert.Equal("in 5 days · Oct 1", upcoming.UpcomingRenewals[0].WhenText);
        Assert.True(upcoming.UpcomingRenewals[0].Soon);
    }

    private static Subscription Renewing(string name, string date)
    {
        var subscription = Spend.Monthly(name, 100);
        subscription.NextRenewal = date;
        return subscription;
    }
}
