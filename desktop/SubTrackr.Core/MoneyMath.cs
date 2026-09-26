using SubTrackr.Core.Contracts;

namespace SubTrackr.Core;

/// <summary>Conversions between the wire <see cref="Money"/> and decimal.</summary>
public static class MoneyMath
{
    /// <summary>Exact decimal value of a Money (minor_units / 10^exponent).</summary>
    public static decimal ToDecimal(this Money money)
    {
        var scale = Pow10(money.Exponent);
        return money.MinorUnits / scale;
    }

    /// <summary>Build a Money from a decimal, rounding half-up to the exponent.</summary>
    public static Money FromDecimal(decimal value, string currency, int exponent = 2)
    {
        var scale = Pow10(exponent);
        var minor = decimal.Round(value * scale, 0, MidpointRounding.AwayFromZero);
        return new Money
        {
            Currency = currency.ToUpperInvariant(),
            MinorUnits = (long)minor,
            Exponent = exponent,
        };
    }

    private static decimal Pow10(int exponent)
    {
        decimal result = 1m;
        for (var i = 0; i < exponent; i++) result *= 10m;
        return result;
    }
}
