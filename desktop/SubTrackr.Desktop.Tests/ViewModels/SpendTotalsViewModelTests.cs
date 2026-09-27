using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class SpendTotalsViewModelTests
{
    [Fact]
    public void Update_CountsOnlyActiveAndTotalsInBaseCurrency()
    {
        var paused = Spend.Monthly("Paused", 999);
        paused.Status = SubStatus.Paused;
        var totals = new SpendTotalsViewModel();

        totals.Update(Spend.Summarize(Spend.Monthly("A", 100), Spend.Monthly("B", 10, "EUR"), paused), "CZK", 0, Spend.Rates);

        Assert.Equal(2, totals.ActiveCount);
        Assert.Equal(Money(350), totals.MonthlyText);
        Assert.Equal(Money(4200), totals.YearlyText);
        Assert.False(totals.HasBudget);
        Assert.Equal("", totals.BudgetText);
    }

    [Fact]
    public void ToggleView_SwitchesTheHeroBetweenMonthAndYear()
    {
        var totals = new SpendTotalsViewModel();
        totals.Update(Spend.Summarize(Spend.Monthly("A", 100)), "CZK", 0, Spend.Rates);
        var changed = new List<string?>();
        totals.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        totals.ToggleViewCommand.Execute(null);

        Assert.Equal("Total per year", totals.HeroCaption);
        Assert.Equal(Formatting.MoneyWhole(1200, "CZK"), totals.HeroAmountText);
        Assert.Contains(nameof(SpendTotalsViewModel.HeroAmountText), changed);
        Assert.Contains(nameof(SpendTotalsViewModel.HeroCaption), changed);
    }

    [Fact]
    public void Update_UnderBudget_ShowsWhatIsLeft()
    {
        var totals = new SpendTotalsViewModel();

        totals.Update(Spend.Summarize(Spend.Monthly("A", 250)), "CZK", 1000, Spend.Rates);

        Assert.False(totals.OverBudget);
        Assert.Equal(0.25, totals.BudgetFraction, 6);
        Assert.Equal(Money(750) + " left", totals.BudgetRemainingText);
        Assert.Equal($"{Money(250)} of {Money(1000)}", totals.BudgetText);
    }

    [Fact]
    public void Update_OverBudget_ShowsTheOverrunAndAFullBar()
    {
        var totals = new SpendTotalsViewModel();

        totals.Update(Spend.Summarize(Spend.Monthly("A", 1200)), "CZK", 1000, Spend.Rates);

        Assert.True(totals.OverBudget);
        Assert.Equal(1, totals.BudgetFraction);
        Assert.Equal(Money(200) + " over", totals.BudgetRemainingText);
    }

    [Fact]
    public void Update_BreaksSpendDownByCurrency()
    {
        var totals = new SpendTotalsViewModel();

        totals.Update(Spend.Summarize(Spend.Monthly("A", 100), Spend.Monthly("B", 10, "EUR")), "CZK", 0, Spend.Rates);

        var euro = totals.CurrencyBreakdown.Single(l => l.Code == "EUR");
        Assert.Equal(Formatting.Money(10, "EUR") + " / mo", euro.MonthlyOwnText);
        Assert.Equal("≈ " + Money(250), euro.ConvertedText);
    }

    private static string Money(decimal amount) => Formatting.Money(amount, "CZK");
}
