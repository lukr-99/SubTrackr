package com.lukr99.subtrackr.model

import kotlinx.serialization.Serializable

/**
 * Kotlin mirror of `Subscription` in contracts/proto/subtrackr.proto. JSON field names match the
 * proto's lowerCamelCase names so data.json keeps the shared shape. Behaviour parity is enforced by
 * the contracts/vectors fixtures.
 */
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
    val worthMode: WorthMode = WorthMode.AUTO,
    val trialEnd: String = "",
    val website: String = "",
    val notes: String = "",
    val createdAt: String = "",
    val updatedAt: String = "",
    val deletedAt: String = "",
)
