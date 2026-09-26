using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SubTrackr.Core;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>Editable working copy for the add/edit dialog.</summary>
public sealed partial class EditSubscriptionViewModel : ObservableObject
{
    private readonly string? _existingId;
    private readonly string? _createdAt;

    public bool IsNew => _existingId is null;
    public string Title => IsNew ? "Add subscription" : "Edit subscription";

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _amount = "0";
    [ObservableProperty] private string _currency = "EUR";
    [ObservableProperty] private CycleOption _selectedCycle;
    [ObservableProperty] private string _customDays = "30";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private string _icon = "💳";
    [ObservableProperty] private bool _autoPay = true;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _usesPerMonth = "0";
    [ObservableProperty] private DateTime _nextRenewal;
    [ObservableProperty] private WorthOption _selectedWorth;
    [ObservableProperty] private bool _isTrial;
    [ObservableProperty] private DateTime _trialEnd;
    [ObservableProperty] private string _website = "";
    [ObservableProperty] private string? _error;

    public IReadOnlyList<WorthOption> WorthModes { get; } = new[]
    {
        new WorthOption(WorthMode.Auto, "Auto (by usage)"),
        new WorthOption(WorthMode.Essential, "Essential"),
        new WorthOption(WorthMode.Worth, "Always worth"),
        new WorthOption(WorthMode.NotWorth, "Not worth"),
    };

    public IReadOnlyList<string> Currencies => Formatting.CommonCurrencies;

    /// <summary>Curated emoji for the icon picker.</summary>
    public IReadOnlyList<string> IconChoices { get; } = new[]
    {
        "🎬", "📺", "🎵", "🎧", "🎮", "🤖", "✳️", "🧠", "💻", "🌐",
        "📱", "☁️", "💳", "🛒", "📦", "🍔", "🍕", "☕", "🥡", "🛵",
        "🚕", "🚊", "🚗", "✈️", "🏋️", "📰", "📚", "🎓", "🔒", "🔑",
        "📸", "🎨", "⚡", "🏠", "🐱", "🐶", "💡", "🎁", "🩺", "💬",
    };

    public IReadOnlyList<CycleOption> Cycles { get; } = new[]
    {
        new CycleOption(BillingCycle.Weekly, "Weekly"),
        new CycleOption(BillingCycle.Monthly, "Monthly"),
        new CycleOption(BillingCycle.Quarterly, "Quarterly"),
        new CycleOption(BillingCycle.Semiannual, "Every 6 months"),
        new CycleOption(BillingCycle.Annual, "Annual"),
        new CycleOption(BillingCycle.CustomDays, "Custom (days)"),
    };

    public bool ShowCustomDays => SelectedCycle?.Cycle == BillingCycle.CustomDays;

    partial void OnSelectedCycleChanged(CycleOption value) => OnPropertyChanged(nameof(ShowCustomDays));

    /// <param name="today">The local date new renewal and trial dates count from.</param>
    /// <param name="existing">The subscription to edit, or null to add one.</param>
    public EditSubscriptionViewModel(DateOnly today, Subscription? existing = null)
    {
        _selectedCycle = Cycles[1]; // Monthly default
        _selectedWorth = WorthModes[0]; // Auto default
        _nextRenewal = today.AddMonths(1).ToDateTime(TimeOnly.MinValue);
        _trialEnd = today.AddDays(14).ToDateTime(TimeOnly.MinValue);
        if (existing is null) return;

        SelectedWorth = WorthModes.FirstOrDefault(w => w.Mode == existing.WorthMode) ?? WorthModes[0];
        Website = existing.Website;
        if (DateOnly.TryParseExact(existing.TrialEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var te))
        {
            IsTrial = true;
            TrialEnd = te.ToDateTime(TimeOnly.MinValue);
        }

        _existingId = existing.Id;
        _createdAt = existing.CreatedAt;
        Name = existing.Name;
        Amount = existing.Cost.ToDecimal().ToString("0.##", CultureInfo.InvariantCulture);
        Currency = existing.Cost.Currency;
        SelectedCycle = Cycles.FirstOrDefault(c => c.Cycle == existing.BillingCycle) ?? Cycles[1];
        CustomDays = existing.CustomDays > 0 ? existing.CustomDays.ToString(CultureInfo.InvariantCulture) : "30";
        Category = existing.Category;
        Icon = string.IsNullOrWhiteSpace(existing.IconRef) ? "💳" : existing.IconRef;
        AutoPay = existing.AutoPay;
        IsPaused = existing.Status == SubStatus.Paused;
        UsesPerMonth = existing.UsesPerMonth.ToString("0.##", CultureInfo.InvariantCulture);
        if (DateOnly.TryParseExact(existing.NextRenewal, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            NextRenewal = d.ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>Validate + build the protobuf message, or null (with Error set) if invalid.</summary>
    public Subscription? TryBuild()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(Name)) { Error = "Name is required."; return null; }
        if (!decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) || amt < 0)
        { Error = "Enter a valid amount."; return null; }

        var days = 0;
        if (SelectedCycle.Cycle == BillingCycle.CustomDays &&
            (!int.TryParse(CustomDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out days) || days <= 0))
        { Error = "Custom interval must be a positive number of days."; return null; }

        double.TryParse(UsesPerMonth, NumberStyles.Number, CultureInfo.InvariantCulture, out var uses);

        var minor = (long)decimal.Round(amt * 100m, 0, MidpointRounding.AwayFromZero);
        return new Subscription
        {
            Id = _existingId ?? Guid.NewGuid().ToString(),
            Name = Name.Trim(),
            Cost = new Money { Currency = Currency.ToUpperInvariant(), MinorUnits = minor, Exponent = 2 },
            BillingCycle = SelectedCycle.Cycle,
            CustomDays = days,
            NextRenewal = DateOnly.FromDateTime(NextRenewal).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Category = Category.Trim(),
            IconRef = Icon.Trim(),
            AutoPay = AutoPay,
            Status = IsPaused ? SubStatus.Paused : SubStatus.Active,
            UsesPerMonth = uses,
            WorthMode = SelectedWorth.Mode,
            Website = Website.Trim(),
            TrialEnd = IsTrial ? DateOnly.FromDateTime(TrialEnd).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "",
            Notes = "",
            CreatedAt = _createdAt ?? "",
            UpdatedAt = "",
            DeletedAt = "",
        };
    }
}
