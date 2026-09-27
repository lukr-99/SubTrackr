using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.Tests.Services;

public class FaviconServiceLogoSourceTests
{
    [Fact]
    public void AddressFor_AsksGoogleForTheTrimmedDomainAt64Pixels()
    {
        var address = FaviconServiceLogoSource.AddressFor("  netflix.com ");

        Assert.Equal("https://www.google.com/s2/favicons?domain=netflix.com&sz=64", address!.AbsoluteUri);
    }
}
