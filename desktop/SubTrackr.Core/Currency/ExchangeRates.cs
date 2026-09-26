using SubTrackr.Core.Diagnostics;

namespace SubTrackr.Core.Currency;

/// <summary>
/// The rate table the app converts with right now. It starts on the built-in table so the first
/// screen needs no network, then refreshes from the live provider; when that fails it keeps the
/// built-in table and says so through <see cref="IsLive"/> (SPEC.md section 3).
/// </summary>
public sealed class ExchangeRates
{
    private readonly IRateProvider provider;
    private readonly TimeProvider time;
    private readonly IAppLog log;

    public ExchangeRates(IRateProvider provider, TimeProvider time, IAppLog log, string baseCurrency)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(log);
        this.provider = provider;
        this.time = time;
        this.log = log;
        Table = OfflineFallback.For(baseCurrency, Today());
    }

    /// <summary>After the table changed, on the thread that changed it.</summary>
    public event EventHandler? Changed;

    public ExchangeRateTable Table { get; private set; }

    /// <summary>Whether <see cref="Table"/> came from the live provider rather than the built-in one.</summary>
    public bool IsLive { get; private set; }

    /// <summary>Switches at once to the built-in table anchored on the new base currency.</summary>
    public void UseOffline(string baseCurrency)
    {
        Table = OfflineFallback.For(baseCurrency, Today());
        IsLive = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Fetches live rates; on failure falls back to the built-in table and logs why.</summary>
    public async Task RefreshAsync(string baseCurrency, CancellationToken cancellationToken)
    {
        try
        {
            Table = await provider.GetRatesAsync(baseCurrency, cancellationToken);
            IsLive = true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            log.Error("Exchange rates could not be refreshed; using the built-in table", exception);
            Table = OfflineFallback.For(baseCurrency, Today());
            IsLive = false;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
