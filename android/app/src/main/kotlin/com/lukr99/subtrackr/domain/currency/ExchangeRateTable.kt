package com.lukr99.subtrackr.domain.currency

import java.math.BigDecimal
import java.math.MathContext

/**
 * Exchange rates anchored to one currency. ratesPerAnchor[X] = units of X per 1 anchor.
 * Conversion routes through the anchor (SPEC.md section 3). Verified by currency-conversion.json.
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
