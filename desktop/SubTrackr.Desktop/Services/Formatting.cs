using System.Collections.Generic;
using System.Globalization;

namespace SubTrackr.Desktop.Services;

/// <summary>Currency display helpers. Keeps formatting in one place.</summary>
public static class Formatting
{
    private static readonly Dictionary<string, string> Symbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = "€",
        ["USD"] = "$",
        ["GBP"] = "£",
        ["CZK"] = "Kč",
        ["PLN"] = "zł",
        ["JPY"] = "¥",
        ["CHF"] = "CHF",
        ["SEK"] = "kr",
        ["NOK"] = "kr",
        ["DKK"] = "kr",
        ["HUF"] = "Ft",
        ["CAD"] = "$",
        ["AUD"] = "$",
    };

    /// <summary>Currencies offered in pickers (superset of what we have offline rates for).</summary>
    public static readonly IReadOnlyList<string> CommonCurrencies = new[]
    {
        "EUR", "USD", "GBP", "CZK", "PLN", "CHF", "SEK", "NOK", "DKK", "HUF", "JPY", "CAD", "AUD",
    };

    public static string Symbol(string currency) =>
        Symbols.TryGetValue(currency, out var s) ? s : currency.ToUpperInvariant();

    /// <summary>e.g. 1234.5 EUR -> "€1,234.50"; suffix-symbol currencies -> "1 234 Kč".</summary>
    public static string Money(decimal amount, string currency, int decimals = 2)
    {
        var sym = Symbol(currency);
        var body = amount.ToString("N" + decimals, CultureInfo.InvariantCulture);
        // Suffix symbols for the koruna/krona/zloty family, prefix otherwise.
        return currency.ToUpperInvariant() switch
        {
            "CZK" or "PLN" or "SEK" or "NOK" or "DKK" or "HUF" => $"{body} {sym}",
            _ => $"{sym}{body}",
        };
    }

    public static string MoneyWhole(decimal amount, string currency) => Money(amount, currency, 0);
}
