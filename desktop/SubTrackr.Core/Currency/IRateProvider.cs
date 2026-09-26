namespace SubTrackr.Core.Currency;

/// <summary>Supplies an <see cref="ExchangeRateTable"/>, live or cached.</summary>
public interface IRateProvider
{
    Task<ExchangeRateTable> GetRatesAsync(string anchor, CancellationToken ct = default);
}
