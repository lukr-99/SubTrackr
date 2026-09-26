using System.Diagnostics;
using SubTrackr.Infrastructure.Tests.Fakes;
using SubTrackr.Infrastructure.Updates;

namespace SubTrackr.Infrastructure.Tests.Updates;

public class InnoSetupLauncherTests
{
    [Fact]
    public void Launch_StartsTheInstallerSilently()
    {
        using var folder = new TemporaryDirectory();
        var installer = folder.File("SubTrackr-Setup-0.3.1.exe");
        File.WriteAllText(installer, "");
        ProcessStartInfo? started = null;
        var launcher = new InnoSetupLauncher(info =>
        {
            started = info;
            return true;
        });

        Assert.True(launcher.Launch(installer));
        Assert.Equal(installer, started!.FileName);
        Assert.Equal("/SILENT /SUPPRESSMSGBOXES /NORESTART", started.Arguments);
        Assert.Equal(folder.Path, started.WorkingDirectory);
    }

    [Fact]
    public void Launch_ProcessDidNotStart_ReturnsFalse()
    {
        using var folder = new TemporaryDirectory();
        var installer = folder.File("SubTrackr-Setup-0.3.1.exe");
        File.WriteAllText(installer, "");

        Assert.False(new InnoSetupLauncher(_ => false).Launch(installer));
    }

    [Fact]
    public void Launch_MissingFile_StartsNothing()
    {
        var calls = 0;
        var launcher = new InnoSetupLauncher(_ =>
        {
            calls++;
            return true;
        });

        Assert.False(launcher.Launch(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".exe")));
        Assert.Equal(0, calls);
    }
}
