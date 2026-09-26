using System.Net;
using SubTrackr.Infrastructure.Currency;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Currency;

public class FrankfurterRateProviderTests
{
    [Fact]
    public async Task GetRatesAsync_ParsesLatestResponse()
    {
        var handler = StubHttpHandler.Text("""{"amount":1.0,"base":"EUR","date":"2026-09-25","rates":{"CZK":24.5,"USD":1.1}}""");
        var provider = new FrankfurterRateProvider(new HttpClient(handler));

        var table = await provider.GetRatesAsync("eur");

        Assert.Equal("https://api.frankfurter.dev/v1/latest?base=EUR", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("EUR", table.Anchor);
        Assert.Equal(new DateOnly(2026, 9, 25), table.Date);
        Assert.Equal(24.5m, table.Convert(1m, "EUR", "CZK"));
    }

    [Fact]
    public async Task GetRatesAsync_ServerError_Throws()
    {
        var provider = new FrankfurterRateProvider(new HttpClient(StubHttpHandler.Text("down", HttpStatusCode.BadGateway)));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetRatesAsync("EUR"));
    }

    [Fact]
    public async Task GetRatesAsync_NonPositiveRate_Throws()
    {
        var handler = StubHttpHandler.Text("""{"date":"2026-09-25","rates":{"CZK":0}}""");
        var provider = new FrankfurterRateProvider(new HttpClient(handler));

        await Assert.ThrowsAsync<InvalidDataException>(() => provider.GetRatesAsync("EUR"));
    }
}
