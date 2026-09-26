using System.Globalization;
using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>Read-only row for the subscriptions list. Wraps a computed <see cref="SubscriptionSpend"/>.</summary>
public sealed class SubscriptionRowViewModel
{
    private readonly SubscriptionSpend _spend;
    private readonly string _baseCurrency;

    public SubscriptionRowViewModel(SubscriptionSpend spend, string baseCurrency)
    {
        _spend = spend;
        _baseCurrency = baseCurrency;
    }

    public Subscription Model => _spend.Subscription;

    public string Icon => string.IsNullOrWhiteSpace(Model.IconRef) ? "•" : Model.IconRef;
    public string Name => Model.Name;

    public bool HasLogo => !string.IsNullOrWhiteSpace(Model.Website);
    public string? LogoUrl => HasLogo
        ? $"https://www.google.com/s2/favicons?domain={Model.Website.Trim()}&sz=64"
        : null;
    public string Category => string.IsNullOrWhiteSpace(Model.Category) ? "Uncategorized" : Model.Category;
    public bool AutoPay => Model.AutoPay;
    public bool IsPaused => Model.Status == SubStatus.Paused;

    public string BillingText => Model.BillingCycle switch
    {
        BillingCycle.Weekly => "Weekly",
        BillingCycle.Monthly => "Monthly",
        BillingCycle.Quarterly => "Quarterly",
        BillingCycle.Semiannual => "Every 6 months",
        BillingCycle.Annual => "Annual",
        BillingCycle.CustomDays => $"Every {Model.CustomDays} days",
        _ => "—",
    };

    public string CostOwnText => Formatting.Money(Model.Cost.ToDecimal(), Model.Cost.Currency);

    public string MonthlyBaseText => Formatting.Money(_spend.MonthlyBase, _baseCurrency);
    public string YearlyBaseText => Formatting.Money(_spend.YearlyBase, _baseCurrency);

    public string NextRenewalText
    {
        get
        {
            if (DateOnly.TryParse(Model.NextRenewal, out var d))
                return d.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
            return "—";
        }
    }

    public string VerdictText => _spend.Verdict switch
    {
        WorthVerdict.Worth => "Worth it",
        WorthVerdict.NotWorth => "Not worth",
        WorthVerdict.Essential => "Essential",
        _ => "—",
    };

    /// <summary>The verdict; the view colors the badge from the theme by it.</summary>
    public WorthVerdict Verdict => _spend.Verdict;

    public bool HasVerdict => _spend.Verdict != WorthVerdict.Unknown;

    // --- sort keys (DataGrid sorts on these; the columns display the *Text versions) ---
    public string NameSort => Name;
    public string CategorySort => Category;
    public decimal CostOwnValue => Model.Cost.ToDecimal();
    public decimal MonthlyBaseValue => _spend.MonthlyBase;
    public int VerdictSort => (int)_spend.Verdict;
    public DateTime NextRenewalDate =>
        DateOnly.TryParse(Model.NextRenewal, out var d) ? d.ToDateTime(TimeOnly.MinValue) : DateTime.MaxValue;
}
