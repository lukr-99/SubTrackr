using SubTrackr.Core.Auth;

namespace SubTrackr.Core.Tests.Auth;

public class EmailAddressTests
{
    [Theory]
    [InlineData(" user@example.com ", "user@example.com")]
    [InlineData("first.last+tag@mail.example", "first.last+tag@mail.example")]
    public void Parse_PlausibleAddress_IsTrimmed(string text, string expected)
    {
        Assert.Equal(expected, EmailAddress.Parse(text)?.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("user")]
    [InlineData("user@example")]
    [InlineData("us er@example.com")]
    public void Parse_Other_IsNull(string text)
    {
        Assert.Null(EmailAddress.Parse(text));
    }
}
