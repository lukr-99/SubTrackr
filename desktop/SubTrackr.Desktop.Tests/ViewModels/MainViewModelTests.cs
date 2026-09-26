using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void Open_WhatIf_BuildsAFreshScenario()
    {
        using var app = TestApp.Create();
        var main = app.Graph.Main;

        main.Open(AppPage.WhatIf);
        var first = main.WhatIf;
        main.Open(AppPage.WhatIf);

        Assert.True(main.IsWhatIfPage);
        Assert.NotNull(first);
        Assert.NotSame(first, main.WhatIf);
    }

    [Fact]
    public void Open_Settings_ResetsUnsavedEdits()
    {
        using var app = TestApp.Create();
        var main = app.Graph.Main;
        app.Graph.Settings.BudgetText = "1";

        main.Open(AppPage.Settings);

        Assert.True(main.IsSettingsPage);
        Assert.Equal("2000", app.Graph.Settings.BudgetText);
    }

    [Fact]
    public void WindowTitle_Release_IsSubTrackr()
    {
        using var app = TestApp.Create(version: "0.3.0");

        Assert.Equal("SubTrackr", app.Graph.Main.WindowTitle);
    }

    [Fact]
    public void WindowTitle_DevBuild_SaysDev()
    {
        using var app = TestApp.Create(version: "0.3.0-dev");

        Assert.Equal("SubTrackr Dev", app.Graph.Main.WindowTitle);
    }
}
