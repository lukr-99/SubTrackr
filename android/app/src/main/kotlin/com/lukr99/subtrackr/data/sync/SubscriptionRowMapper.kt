package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.WorthMode
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put

/**
 * A [Subscription] as a row of the `subscriptions` table (SPEC.md section 8.3), verified by
 * contracts/vectors/sync-rows.json. Enums are their proto names; money is three columns. Reading
 * maps a missing or unknown enum to MONTHLY, ACTIVE, or AUTO and any other missing column to empty
 * text, zero, or false. A column of the wrong type throws IllegalArgumentException.
 */
object SubscriptionRowMapper {

    fun toRow(s: Subscription, userId: String): JsonObject = buildJsonObject {
        put("user_id", userId)
        put("id", s.id)
        put("name", s.name)
        put("cost_currency", s.cost.currency)
        put("cost_minor", s.cost.minorUnits)
        put("cost_exponent", s.cost.exponent)
        put("billing_cycle", s.billingCycle.name)
        put("custom_days", s.customDays)
        put("next_renewal", s.nextRenewal)
        put("category", s.category)
        put("icon_ref", s.iconRef)
        put("auto_pay", s.autoPay)
        put("status", s.status.name)
        put("uses_per_month", s.usesPerMonth)
        put("worth_mode", s.worthMode.name)
        put("trial_end", s.trialEnd)
        put("website", s.website)
        put("notes", s.notes)
        put("created_at", s.createdAt)
        put("updated_at", s.updatedAt)
        put("deleted_at", s.deletedAt)
    }

    fun fromRow(row: JsonObject): Subscription = Subscription(
        id = row.text("id"),
        name = row.text("name"),
        cost = Money(
            currency = row.text("cost_currency"),
            minorUnits = row.number("cost_minor")?.toLongOrNull() ?: 0,
            exponent = row.number("cost_exponent")?.toIntOrNull() ?: 0,
        ),
        billingCycle = row.enum("billing_cycle", BillingCycle.MONTHLY, BillingCycle.BILLING_CYCLE_UNSPECIFIED),
        customDays = row.number("custom_days")?.toIntOrNull() ?: 0,
        nextRenewal = row.text("next_renewal"),
        category = row.text("category"),
        iconRef = row.text("icon_ref"),
        autoPay = row.flag("auto_pay"),
        status = row.enum("status", SubStatus.ACTIVE, SubStatus.SUB_STATUS_UNSPECIFIED),
        usesPerMonth = row.number("uses_per_month")?.toDoubleOrNull() ?: 0.0,
        notes = row.text("notes"),
        worthMode = row.enum("worth_mode", WorthMode.AUTO, null),
        trialEnd = row.text("trial_end"),
        website = row.text("website"),
        createdAt = row.text("created_at"),
        updatedAt = row.text("updated_at"),
        deletedAt = row.text("deleted_at"),
    )

    private fun JsonObject.value(column: String): JsonPrimitive? = when (val v: JsonElement? = this[column]) {
        null, JsonNull -> null
        is JsonPrimitive -> v
        else -> throw IllegalArgumentException("column $column is not a scalar")
    }

    private fun JsonObject.text(column: String): String {
        val v = value(column) ?: return ""
        require(v.isString) { "column $column is not text" }
        return v.content
    }

    /** Numbers arrive as JSON numbers; bigint may also arrive as text. */
    private fun JsonObject.number(column: String): String? {
        val v = value(column) ?: return null
        requireNotNull(v.content.toBigDecimalOrNull()) { "column $column is not a number" }
        return v.content.toBigDecimal().let {
            if (it.stripTrailingZeros().scale() <= 0) it.toBigIntegerExact().toString() else it.toPlainString()
        }
    }

    private fun JsonObject.flag(column: String): Boolean {
        val v = value(column) ?: return false
        require(!v.isString && (v.content == "true" || v.content == "false")) { "column $column is not a boolean" }
        return v.content == "true"
    }

    /** Unknown names, the proto's UNSPECIFIED value, and missing columns all read as [fallback]. */
    private inline fun <reified E : Enum<E>> JsonObject.enum(column: String, fallback: E, unspecified: E?): E {
        val name = value(column)?.takeIf { it.isString }?.content ?: return fallback
        val parsed = enumValues<E>().firstOrNull { it.name == name } ?: return fallback
        return if (parsed == unspecified) fallback else parsed
    }
}
