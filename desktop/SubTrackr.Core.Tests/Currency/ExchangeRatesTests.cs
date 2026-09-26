using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Currency;

public class ExchangeRatesTests
{
    private readonly FakeTimeProvider time = new(Samples.Now);

    [Fact]
    public void Constructor_StartsOnBuiltInTable()
    {
        var rates = new ExchangeRates(new StubRateProvider(), time, new RecordingLog(), "CZK");

        Assert.Equal("CZK", rates.Table.Anchor);
        Assert.False(rates.IsLive);
        Assert.Equal(new DateOnly(2026, 9, 26), rates.Table.Date);
    }

    [Fact]
    public async Task RefreshAsync_ProviderAnswers_UsesLiveTable()
    {
        var rates = new ExchangeRates(new StubRateProvider(), time, new RecordingLog(), "EUR");
        var changed = 0;
        rates.Changed += (_, _) => changed++;

        await rates.RefreshAsync("EUR", CancellationToken.None);

        Assert.True(rates.IsLive);
        Assert.Equal(new DateOnly(2026, 9, 25), rates.Table.Date);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task RefreshAsync_ProviderFails_FallsBackAndLogs()
    {
        var provider = new StubRateProvider { Failure = new HttpRequestException("offline") };
        var log = new RecordingLog();
        var rates = new ExchangeRates(provider, time, log, "EUR");

        await rates.RefreshAsync("USD", CancellationToken.None);

        Assert.False(rates.IsLive);
        Assert.Equal("USD", rates.Table.Anchor);
        Assert.Contains(log.Lines, line => line.StartsWith("ERROR", StringComparison.Ordinal));
    }

    [Fact]
    public void UseOffline_ReanchorsAtOnce()
    {
        var rates = new ExchangeRates(new StubRateProvider(), time, new RecordingLog(), "EUR");

        rates.UseOffline("GBP");

        Assert.Equal("GBP", rates.Table.Anchor);
        Assert.True(rates.Table.Knows("CZK"));
    }
}
