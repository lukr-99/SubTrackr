using SubTrackr.Core.Updates;

namespace SubTrackr.Core.Tests.Updates;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("0.3.0", 0, 3, 0, "")]
    [InlineData("0.3.0-dev", 0, 3, 0, "dev")]
    [InlineData(" 10.20.30 ", 10, 20, 30, "")]
    public void TryParse_ValidVersions(string text, int major, int minor, int patch, string preRelease)
    {
        Assert.Equal(new ReleaseVersion(major, minor, patch, preRelease), ReleaseVersion.TryParse(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("v0.3.0")]
    [InlineData("0.3")]
    [InlineData("0.3.0+abc")]
    [InlineData(null)]
    public void TryParse_Invalid_ReturnsNull(string? text)
    {
        Assert.Null(ReleaseVersion.TryParse(text));
    }

    [Theory]
    [InlineData("v0.3.1", true)]
    [InlineData("0.3.1", false)]
    [InlineData("v0.3.1-beta", false)]
    [InlineData("nightly", false)]
    public void TryParseTag_RequiresPlainVTag(string tag, bool valid)
    {
        Assert.Equal(valid, ReleaseVersion.TryParseTag(tag) is not null);
    }

    [Fact]
    public void Compare_UsesNumbersAndPutsPreReleasesFirst()
    {
        var older = ReleaseVersion.TryParse("0.9.0")!;
        var newer = ReleaseVersion.TryParse("0.10.0")!;
        var dev = ReleaseVersion.TryParse("0.10.0-dev")!;

        Assert.True(newer > older);
        Assert.True(dev < newer);
        Assert.True(dev > older);
    }

    [Theory]
    [InlineData("0.3.0", true)]
    [InlineData("0.3.0-dev", false)]
    [InlineData("garbage", false)]
    public void VersionPolicy_ChecksAllowed_OnlyForReleases(string running, bool allowed)
    {
        Assert.Equal(allowed, VersionPolicy.ChecksAllowed(running));
    }
}
