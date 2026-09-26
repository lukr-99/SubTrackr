package com.lukr99.subtrackr.domain.currency

import java.math.BigDecimal
import java.math.MathContext

/** Last-resort static rates (units per 1 EUR) so the app works offline on first run. */
object OfflineFallback {
    private val PER_EUR: Map<String, BigDecimal> = mapOf(
        "EUR" to BigDecimal.ONE,
        "USD" to BigDecimal("1.09"),
        "CZK" to BigDecimal("25.10"),
        "GBP" to BigDecimal("0.85"),
        "PLN" to BigDecimal("4.30"),
        "CHF" to BigDecimal("0.95"),
        "JPY" to BigDecimal("170"),
        "CAD" to BigDecimal("1.48"),
        "AUD" to BigDecimal("1.64"),
        "SEK" to BigDecimal("11.30"),
        "NOK" to BigDecimal("11.60"),
        "DKK" to BigDecimal("7.46"),
        "HUF" to BigDecimal("395"),
    )

    fun forAnchor(anchor: String): ExchangeRateTable {
        val a = anchor.uppercase()
        val anchorPerEur = PER_EUR[a] ?: BigDecimal.ONE
        val reanchored = PER_EUR.mapValues { (_, perEur) ->
            perEur.divide(anchorPerEur, MathContext.DECIMAL64)
        }
        return ExchangeRateTable(a, reanchored, "offline")
    }
}
