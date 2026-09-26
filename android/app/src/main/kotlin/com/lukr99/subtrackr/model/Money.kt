package com.lukr99.subtrackr.model

import kotlinx.serialization.Serializable
import java.math.BigDecimal
import java.math.MathContext
import java.math.RoundingMode

/** Money as integer minor units plus exponent, never a float: value = minorUnits / 10^exponent. */
@Serializable
data class Money(
    val currency: String = "EUR",
    val minorUnits: Long = 0,
    val exponent: Int = 2,
) {
    fun toBigDecimal(): BigDecimal =
        BigDecimal(minorUnits).divide(BigDecimal.TEN.pow(exponent), MathContext.DECIMAL64)

    companion object {
        fun of(amount: BigDecimal, currency: String, exponent: Int = 2): Money {
            val minor = amount.movePointRight(exponent).setScale(0, RoundingMode.HALF_UP)
            return Money(currency.uppercase(), minor.toLong(), exponent)
        }
    }
}
