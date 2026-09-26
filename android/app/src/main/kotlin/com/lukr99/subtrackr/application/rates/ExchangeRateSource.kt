package com.lukr99.subtrackr.application.rates

import com.lukr99.subtrackr.domain.currency.ExchangeRateTable

/** Where live exchange rates come from. The Frankfurter adapter implements it; tests use a fake. */
fun interface ExchangeRateSource {
    /** Rates anchored to [anchor]. Implementations fall back to offline rates instead of throwing. */
    suspend fun latest(anchor: String): ExchangeRateTable
}
