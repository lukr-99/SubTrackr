using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class WhatIfViewModelTests
{
    [Fact]
    public void Amount_InBaseCurrency_AddsToTheCurrentTotal()
    {
        using var app = TestApp.Create();
        app.Graph.Main.Open(AppPage.WhatIf);
        var whatIf = app.Graph.Main.WhatIf!;

        whatIf.Currency = "CZK";
        whatIf.SelectedCycle = whatIf.Cycles.Single(c => c.Cycle == BillingCycle.Monthly);
        whatIf.Amount = "100";

        Assert.Equal("+ 100.00 Kč", whatIf.AddedMonthlyText);
    }

    [Fact]
    public void UsesPerMonth_BelowThreshold_SaysWorthIt()
    {
        using var app = TestApp.Create();
        app.Graph.Main.Open(AppPage.WhatIf);
        var whatIf = app.Graph.Main.WhatIf!;

        whatIf.Currency = "CZK";
        whatIf.Amount = "100";
        whatIf.UsesPerMonth = "10";

        Assert.Equal("10.00 Kč per use", whatIf.CostPerUseText);
        Assert.StartsWith("Worth it", whatIf.VerdictText, StringComparison.Ordinal);
    }

    [Fact]
    public void Amount_NotANumber_SaysSo()
    {
        using var app = TestApp.Create();
        app.Graph.Main.Open(AppPage.WhatIf);
        var whatIf = app.Graph.Main.WhatIf!;

        whatIf.Amount = "abc";

        Assert.Equal("Enter a valid amount", whatIf.DeltaText);
    }
}
