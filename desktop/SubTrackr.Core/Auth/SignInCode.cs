namespace SubTrackr.Core.Auth;

/// <summary>
/// The one-time code Supabase Auth emails for sign-in. Projects send 6 digits by default and can
/// be set to up to 10, so 6 to 10 ASCII digits are accepted; spaces are ignored.
/// </summary>
public sealed record SignInCode
{
    public const int MinLength = 6;
    public const int MaxLength = 10;

    private SignInCode(string value) => Value = value;

    public string Value { get; }

    public static SignInCode? Parse(string? text)
    {
        var digits = string.Concat((text ?? "").Where(character => !char.IsWhiteSpace(character)));
        return digits.Length is >= MinLength and <= MaxLength && digits.All(char.IsAsciiDigit)
            ? new SignInCode(digits)
            : null;
    }

    public override string ToString() => Value;
}
