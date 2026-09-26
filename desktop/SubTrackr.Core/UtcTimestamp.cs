using System.Globalization;

namespace SubTrackr.Core;

/// <summary>
/// The one timestamp format SubTrackr writes: ISO-8601 in UTC with a trailing Z
/// (<c>2026-08-28T10:00:00.0000000Z</c>), so ordinal string comparison equals time order.
/// </summary>
public static class UtcTimestamp
{
    public static string Format(DateTimeOffset moment) =>
        moment.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);

    public static string Now(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        return Format(time.GetUtcNow());
    }
}
