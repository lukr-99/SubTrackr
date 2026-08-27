using System.Globalization;
using System.Windows.Media;
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
        _ => "—",
    };

    public Brush VerdictBrush => _spend.Verdict switch
    {
        WorthVerdict.Worth => new SolidColorBrush(Color.FromRgb(0x3D, 0xD6, 0x8C)),
        WorthVerdict.NotWorth => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
        _ => new SolidColorBrush(Color.FromRgb(0x6C, 0x72, 0x7A)),
    };

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
