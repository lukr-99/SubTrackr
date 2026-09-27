using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// What is about to happen to active subscriptions: alerts for trials ending within a week and
/// renewals within three days, and the next five renewals. Paused subscriptions never show here.
/// </summary>
public sealed class UpcomingViewModel : ObservableObject
{
    public ObservableCollection<AlertLine> Alerts { get; } = [];

    public ObservableCollection<RenewalLine> UpcomingRenewals { get; } = [];

    public bool HasAlerts => Alerts.Count > 0;

    /// <summary>Rebuilds the alerts and renewals, counting days from <paramref name="today"/>.</summary>
    public void Update(SpendSummary summary, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var active = summary.PerSub.Where(p => p.Subscription.Status == SubStatus.Active).Select(p => p.Subscription).ToList();
        BuildRenewals(active, today);
        BuildAlerts(active, today);
    }

    private static DateOnly? ParseDate(string text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static string When(int days) => days <= 0 ? "today" : days == 1 ? "tomorrow" : $"in {days} days";

    private static string Price(Subscription s) => Formatting.Money(s.Cost.ToDecimal(), s.Cost.Currency);

    private void BuildRenewals(List<Subscription> active, DateOnly today)
    {
        UpcomingRenewals.Clear();
        var upcoming = active
            .Select(s => (Subscription: s, Date: ParseDate(s.NextRenewal)))
            .Where(x => x.Date is not null)
            .OrderBy(x => x.Date)
            .Take(5);

        foreach (var (subscription, date) in upcoming)
        {
            var days = date!.Value.DayNumber - today.DayNumber;
            var when = days <= 0 ? "due" : days == 1 ? "tomorrow" : $"in {days} days";
            UpcomingRenewals.Add(new RenewalLine(
                string.IsNullOrWhiteSpace(subscription.IconRef) ? "•" : subscription.IconRef,
                subscription.Name,
                $"{when} · {date.Value.ToString("MMM d", CultureInfo.InvariantCulture)}",
                Price(subscription),
                days <= 7));
        }
    }

    private void BuildAlerts(List<Subscription> active, DateOnly today)
    {
        Alerts.Clear();
        foreach (var s in active)
        {
            if (ParseDate(s.TrialEnd) is { } trialEnd)
            {
                var days = trialEnd.DayNumber - today.DayNumber;
                if (days is >= 0 and <= 7)
                {
                    Alerts.Add(new AlertLine("🆓", $"{s.Name} trial ends", $"{When(days)} · then {Price(s)}", days <= 2));
                }
            }

            if (ParseDate(s.NextRenewal) is { } renewal)
            {
                var days = renewal.DayNumber - today.DayNumber;
                if (days is >= 0 and <= 3)
                {
                    Alerts.Add(new AlertLine(string.IsNullOrWhiteSpace(s.IconRef) ? "🔔" : s.IconRef,
                        $"{s.Name} renews", $"{When(days)} · {Price(s)}", days <= 1));
                }
            }
        }

        OnPropertyChanged(nameof(HasAlerts));
    }
}
