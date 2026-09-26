using SubTrackr.Core.Currency;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>Answers with a fixed table, or throws when <see cref="Failure"/> is set.</summary>
public sealed class StubRateProvider : IRateProvider
{
    public Exception? Failure { get; set; }

    public int Calls { get; private set; }

    public Task<ExchangeRateTable> GetRatesAsync(string anchor, CancellationToken ct = default)
    {
        Calls++;
        if (Failure is not null)
        {
            return Task.FromException<ExchangeRateTable>(Failure);
        }

        var rates = new Dictionary<string, decimal> { ["USD"] = 1.25m, ["CZK"] = 25m };
        return Task.FromResult(new ExchangeRateTable(anchor, rates, new DateOnly(2026, 9, 25)));
    }
}
