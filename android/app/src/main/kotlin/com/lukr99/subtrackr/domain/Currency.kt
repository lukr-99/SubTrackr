package com.lukr99.subtrackr.domain

import java.math.BigDecimal
import java.math.MathContext

/**
 * Exchange rates anchored to one currency. ratesPerAnchor[X] = units of X per 1 anchor.
 * Conversion routes through the anchor (SPEC.md §3). Verified by currency-conversion.json.
 */
class ExchangeRateTable(
    anchor: String,
    ratesPerAnchor: Map<String, BigDecimal>,
    val date: String,
) {
    val anchor: String = anchor.uppercase()
    private val rates: Map<String, BigDecimal> =
        (mapOf(this.anchor to BigDecimal.ONE) + ratesPerAnchor.mapKeys { it.key.uppercase() })

    fun knows(currency: String): Boolean = rates.containsKey(currency.uppercase())

    val currencies: Set<String> get() = rates.keys

    fun convert(amount: BigDecimal, from: String, to: String): BigDecimal {
        val f = from.uppercase()
        val t = to.uppercase()
        if (f == t) return amount
        return amount.divide(rateOf(f), MathContext.DECIMAL64).multiply(rateOf(t))
    }

    private fun rateOf(currency: String): BigDecimal =
        rates[currency] ?: throw NoSuchElementException("No exchange rate for '$currency' (anchor $anchor).")
}

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
