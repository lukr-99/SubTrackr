package com.lukr99.subtrackr.model

import kotlinx.serialization.Serializable
import java.math.BigDecimal
import java.math.RoundingMode

/**
 * Money as integer minor units plus exponent, never a float: value = minorUnits / 10^exponent.
 *
 * The Kotlin defaults (EUR, 2) serve new entries in the app. Backups and sync rows decode missing
 * fields to the proto defaults ("", 0, 0) instead; see ProtoJson.
 */
@Serializable
data class Money(
    val currency: String = "EUR",
    val minorUnits: Long = 0,
    val exponent: Int = 2,
) {
    /** Exact for any exponent, including out-of-range ones from foreign data. */
    fun toBigDecimal(): BigDecimal = BigDecimal.valueOf(minorUnits, exponent)

    companion object {
        fun of(amount: BigDecimal, currency: String, exponent: Int = 2): Money {
            val minor = amount.movePointRight(exponent).setScale(0, RoundingMode.HALF_UP)
            return Money(currency.uppercase(), minor.toLong(), exponent)
        }
    }
}
