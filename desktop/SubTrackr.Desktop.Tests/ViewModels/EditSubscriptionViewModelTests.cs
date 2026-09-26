using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class EditSubscriptionViewModelTests
{
    private static readonly DateOnly Today = new(2026, 9, 26);

    [Fact]
    public void Constructor_NewSubscription_DefaultsFromToday()
    {
        var editor = new EditSubscriptionViewModel(Today);

        Assert.True(editor.IsNew);
        Assert.Equal(new DateTime(2026, 10, 26), editor.NextRenewal);
        Assert.Equal(new DateTime(2026, 10, 10), editor.TrialEnd);
    }

    [Fact]
    public void TryBuild_MissingName_ReportsError()
    {
        var editor = new EditSubscriptionViewModel(Today) { Amount = "5" };

        Assert.Null(editor.TryBuild());
        Assert.Equal("Name is required.", editor.Error);
    }

    [Fact]
    public void TryBuild_CustomCycleWithoutDays_ReportsError()
    {
        var editor = new EditSubscriptionViewModel(Today) { Name = "Gym", Amount = "30" };
        editor.SelectedCycle = editor.Cycles.Single(c => c.Cycle == BillingCycle.CustomDays);
        editor.CustomDays = "0";

        Assert.Null(editor.TryBuild());
        Assert.Equal("Custom interval must be a positive number of days.", editor.Error);
    }

    [Fact]
    public void TryBuild_Existing_KeepsIdentityAndDates()
    {
        var existing = new Subscription
        {
            Id = "11111111-1111-4111-8111-111111111111",
            Name = "Alpha",
            Cost = new Money { Currency = "USD", MinorUnits = 1250, Exponent = 2 },
            BillingCycle = BillingCycle.Annual,
            NextRenewal = "2027-01-15",
            TrialEnd = "2026-10-01",
            CreatedAt = "2026-09-01T08:00:00Z",
        };
        var editor = new EditSubscriptionViewModel(Today, existing);

        var built = editor.TryBuild();

        Assert.NotNull(built);
        Assert.Equal(existing.Id, built.Id);
        Assert.Equal(1250, built.Cost.MinorUnits);
        Assert.Equal(BillingCycle.Annual, built.BillingCycle);
        Assert.Equal("2027-01-15", built.NextRenewal);
        Assert.Equal("2026-10-01", built.TrialEnd);
        Assert.Equal("2026-09-01T08:00:00Z", built.CreatedAt);
    }
}
