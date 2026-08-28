package com.lukr99.subtrackr.model

import kotlinx.serialization.Serializable
import java.math.BigDecimal
import java.math.MathContext

/**
 * Kotlin mirror of contracts/proto/subtrackr.proto. The .proto is the schema of record; these
 * @Serializable classes mirror it (JSON field names match the proto's camelCase) so storage stays
 * sync-compatible with the WPF app. Behaviour parity is enforced by the contracts/vectors fixtures.
 */

enum class BillingCycle {
    BILLING_CYCLE_UNSPECIFIED,
    WEEKLY,
    MONTHLY,
    QUARTERLY,
    SEMIANNUAL,
    ANNUAL,
    CUSTOM_DAYS,
}

enum class SubStatus {
    SUB_STATUS_UNSPECIFIED,
    ACTIVE,
    PAUSED,
}

/** Money as integer minor units + exponent (never a float). value = minorUnits / 10^exponent. */
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
            val minor = amount.movePointRight(exponent).setScale(0, java.math.RoundingMode.HALF_UP)
            return Money(currency.uppercase(), minor.toLong(), exponent)
        }
    }
}

@Serializable
data class Subscription(
    val id: String = "",
    val name: String = "",
    val cost: Money = Money(),
    val billingCycle: BillingCycle = BillingCycle.MONTHLY,
    val customDays: Int = 0,
    val nextRenewal: String = "",
    val category: String = "",
    val iconRef: String = "",
    val autoPay: Boolean = false,
    val status: SubStatus = SubStatus.ACTIVE,
    val usesPerMonth: Double = 0.0,
    val notes: String = "",
    val createdAt: String = "",
    val updatedAt: String = "",
    val deletedAt: String = "",
)

@Serializable
data class Settings(
    val baseCurrency: String = "EUR",
    val schemaVersion: String = "0.1",
)

@Serializable
data class Database(
    val schemaVersion: String = "0.1",
    val settings: Settings = Settings(),
    val subscriptions: List<Subscription> = emptyList(),
)
