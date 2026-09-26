using SubTrackr.Infrastructure.Storage;

namespace SubTrackr.Infrastructure.Tests.Storage;

public class AppDataPathsTests
{
    [Fact]
    public void Files_LiveUnderTheRoot()
    {
        var paths = new AppDataPaths(Path.Combine("root", "SubTrackr"));

        Assert.Equal(Path.Combine(paths.Root, "data.json"), paths.DataFile);
        Assert.Equal(Path.Combine(paths.Root, "logs"), paths.LogsFolder);
    }

    [Fact]
    public void ForUser_UsesRoamingAppData()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SubTrackr");

        Assert.Equal(expected, AppDataPaths.ForUser().Root);
    }
}
