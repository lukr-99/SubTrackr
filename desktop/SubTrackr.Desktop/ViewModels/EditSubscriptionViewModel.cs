using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SubTrackr.Core;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

public sealed record CycleOption(BillingCycle Cycle, string Label)
{
    // The ComboBox renders items via ToString(); show the friendly label, not the record dump.
    public override string ToString() => Label;
}

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
    [ObservableProperty] private DateTime _nextRenewal = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string? _error;

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

    public EditSubscriptionViewModel(Subscription? existing = null)
    {
        _selectedCycle = Cycles[1]; // Monthly default
        if (existing is null) return;

        _existingId = existing.Id;
        _createdAt = existing.CreatedAt;
        Name = existing.Name;
        Amount = existing.Cost.ToDecimal().ToString("0.##", CultureInfo.InvariantCulture);
        Currency = existing.Cost.Currency;
        SelectedCycle = Cycles.FirstOrDefault(c => c.Cycle == existing.BillingCycle) ?? Cycles[1];
        CustomDays = existing.CustomDays > 0 ? existing.CustomDays.ToString() : "30";
        Category = existing.Category;
        Icon = string.IsNullOrWhiteSpace(existing.IconRef) ? "💳" : existing.IconRef;
        AutoPay = existing.AutoPay;
        IsPaused = existing.Status == SubStatus.Paused;
        UsesPerMonth = existing.UsesPerMonth.ToString("0.##", CultureInfo.InvariantCulture);
        if (DateOnly.TryParse(existing.NextRenewal, out var d))
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
            (!int.TryParse(CustomDays, out days) || days <= 0))
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
            NextRenewal = DateOnly.FromDateTime(NextRenewal).ToString("yyyy-MM-dd"),
            Category = Category.Trim(),
            IconRef = Icon.Trim(),
            AutoPay = AutoPay,
            Status = IsPaused ? SubStatus.Paused : SubStatus.Active,
            UsesPerMonth = uses,
            Notes = "",
            CreatedAt = _createdAt ?? "",
            UpdatedAt = "",
            DeletedAt = "",
        };
    }
}
