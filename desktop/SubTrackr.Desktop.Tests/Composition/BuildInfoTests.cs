using SubTrackr.Desktop.Composition;

namespace SubTrackr.Desktop.Tests.Composition;

public class BuildInfoTests
{
    [Fact]
    public void Release_UsesTheInstalledIdentity()
    {
        var build = new BuildInfo("0.3.0");

        Assert.False(build.IsDevBuild);
        Assert.Equal("SubTrackr", build.ProductName);
        Assert.Equal("SubTrackr_SingleInstance_7f3a", build.InstanceMutexName);
    }

    [Fact]
    public void DevBuild_HasItsOwnIdentity()
    {
        var build = new BuildInfo("0.3.0-dev");

        Assert.True(build.IsDevBuild);
        Assert.Equal("SubTrackr Dev", build.ProductName);
        Assert.Equal("SubTrackr_Dev_SingleInstance_7f3a", build.InstanceMutexName);
    }

    [Fact]
    public void FromAssembly_ThisTestBuild_IsADevBuild()
    {
        // Every build without -p:SubTrackrReleaseBuild=true is X.Y.Z-dev (Directory.Build.props).
        var build = BuildInfo.FromAssembly(typeof(App).Assembly);

        Assert.EndsWith("-dev", build.Version, StringComparison.Ordinal);
        Assert.True(build.IsDevBuild);
    }
}
