package com.lukr99.subtrackr.ui

import java.math.BigDecimal
import java.math.RoundingMode
import java.text.DecimalFormat
import java.text.DecimalFormatSymbols
import java.util.Locale

/** Currency display helpers (mirrors the WPF Formatting helper). */
object Format {
    private val symbols = mapOf(
        "EUR" to "€", "USD" to "$", "GBP" to "£", "CZK" to "Kč", "PLN" to "zł",
        "JPY" to "¥", "CHF" to "CHF", "SEK" to "kr", "NOK" to "kr", "DKK" to "kr",
        "HUF" to "Ft", "CAD" to "$", "AUD" to "$",
    )

    val commonCurrencies = listOf(
        "EUR", "USD", "GBP", "CZK", "PLN", "CHF", "SEK", "NOK", "DKK", "HUF", "JPY", "CAD", "AUD",
    )

    fun symbol(currency: String) = symbols[currency.uppercase()] ?: currency.uppercase()

    fun money(amount: BigDecimal, currency: String, decimals: Int = 2): String {
        val df = DecimalFormat().apply {
            decimalFormatSymbols = DecimalFormatSymbols(Locale.US).apply { groupingSeparator = ',' }
            minimumFractionDigits = decimals
            maximumFractionDigits = decimals
            isGroupingUsed = true
        }
        val body = df.format(amount.setScale(decimals, RoundingMode.HALF_UP))
        val sym = symbol(currency)
        return when (currency.uppercase()) {
            "CZK", "PLN", "SEK", "NOK", "DKK", "HUF" -> "$body $sym"
            else -> "$sym$body"
        }
    }

    fun moneyWhole(amount: BigDecimal, currency: String) = money(amount, currency, 0)
}
