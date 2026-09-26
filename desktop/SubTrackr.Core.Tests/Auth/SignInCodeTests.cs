using SubTrackr.Core.Auth;

namespace SubTrackr.Core.Tests.Auth;

public class SignInCodeTests
{
    [Theory]
    [InlineData("123456", "123456")]
    [InlineData(" 123 456 ", "123456")]
    [InlineData("1234567890", "1234567890")]
    public void Parse_SixToTenDigits(string text, string expected)
    {
        Assert.Equal(expected, SignInCode.Parse(text)?.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("12345678901")]
    [InlineData("１２３４５６")]
    [InlineData(null)]
    public void Parse_Other_IsNull(string? text)
    {
        Assert.Null(SignInCode.Parse(text));
    }
}
