using SubTrackr.Infrastructure.Storage;

namespace SubTrackr.Infrastructure.Tests.Storage;

public class AppDataPathsTests
{
    private static readonly string RoamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    [Fact]
    public void Files_LiveUnderTheRoot()
    {
        var paths = new AppDataPaths(Path.Combine("root", "SubTrackr"));

        Assert.Equal(Path.Combine(paths.Root, "data.json"), paths.DataFile);
        Assert.Equal(Path.Combine(paths.Root, "logs"), paths.LogsFolder);
    }

    [Fact]
    public void ForUser_Release_UsesTheInstalledAppFolder()
    {
        Assert.Equal(Path.Combine(RoamingAppData, "SubTrackr"), AppDataPaths.ForUser(isDevBuild: false).Root);
    }

    [Fact]
    public void ForUser_DevBuild_KeepsItsDataApart()
    {
        Assert.Equal(Path.Combine(RoamingAppData, "SubTrackr Dev"), AppDataPaths.ForUser(isDevBuild: true).Root);
    }
}
