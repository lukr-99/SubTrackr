using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.Tests.Fakes;

namespace SubTrackr.Desktop.Tests.Services;

public class UpdatePromptTests
{
    [Fact]
    public async Task CheckAndOfferAsync_UpToDateManualCheck_SaysSo()
    {
        var dialogs = new RecordingDialogs();
        var prompt = new UpdatePrompt(new FakeUpdater(), "0.3.0", dialogs, new RecordingDesktop(), new RecordingLog());

        await prompt.CheckAndOfferAsync(announceUpToDate: true);

        Assert.Equal(["You're on the latest version."], dialogs.Messages);
    }

    [Fact]
    public async Task CheckAndOfferAsync_Accepted_InstallsThenExits()
    {
        var updater = new FakeUpdater { Offered = "0.3.1" };
        var desktop = new RecordingDesktop();
        var prompt = new UpdatePrompt(updater, "0.3.0", new RecordingDialogs(), desktop, new RecordingLog());

        await prompt.CheckAndOfferAsync(announceUpToDate: false);

        Assert.Equal(1, updater.Installs);
        Assert.Equal(1, desktop.Shutdowns);
    }

    [Fact]
    public async Task CheckAndOfferAsync_Declined_DoesNothing()
    {
        var updater = new FakeUpdater { Offered = "0.3.1" };
        var desktop = new RecordingDesktop();
        var prompt = new UpdatePrompt(updater, "0.3.0", new RecordingDialogs { ConfirmAnswer = false }, desktop, new RecordingLog());

        await prompt.CheckAndOfferAsync(announceUpToDate: false);

        Assert.Equal(0, updater.Installs);
        Assert.Equal(0, desktop.Shutdowns);
    }
}
