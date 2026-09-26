using SubTrackr.Core.Currency;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Always answers with the built-in table, dated a fixed day.</summary>
public sealed class FixedRateProvider : IRateProvider
{
    public Task<ExchangeRateTable> GetRatesAsync(string anchor, CancellationToken ct = default) =>
        Task.FromResult(OfflineFallback.For(anchor, new DateOnly(2026, 9, 25)));
}
