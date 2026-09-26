namespace SubTrackr.Core.Updates;

/// <summary>
/// Reads a <c>.sha256</c> file: 64 hex digits, optionally followed by whitespace and a file name
/// (the <c>sha256sum</c> layout).
/// </summary>
public static class ChecksumFile
{
    private const int HexLength = 64;

    /// <summary>The lowercase hex SHA-256, or null when the text does not start with one.</summary>
    public static string? Parse(string? text)
    {
        var trimmed = (text ?? "").Trim();
        var end = 0;
        while (end < trimmed.Length && !char.IsWhiteSpace(trimmed[end]))
        {
            end++;
        }

        var hash = trimmed[..end];
        return hash.Length == HexLength && hash.All(char.IsAsciiHexDigit)
            ? hash.ToLowerInvariant()
            : null;
    }
}
