package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.model.WorthMode
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.math.BigDecimal

/**
 * The proto3 JSON mapping of the shared data contract (SPEC.md section 9.1), written by hand so it
 * follows the proto rather than the Kotlin constructor defaults:
 *
 * - writing emits every field, enum names as strings, and `minorUnits` (int64) as a JSON string;
 * - reading accepts lowerCamelCase or proto field names, 64-bit and 32-bit integers as strings or
 *   numbers, enum names or numbers, ignores unknown fields, and gives missing or null fields their
 *   proto defaults (empty text, zero, false, the enum's zero value).
 *
 * Wrong value types throw [ProtoJsonException].
 */
object ProtoJson {
    private const val MAX_EXACT_DOUBLE = 9_007_199_254_740_992.0 // 2^53

    fun encodeDatabase(db: Database): JsonObject = buildJsonObject {
        put("schemaVersion", db.schemaVersion)
        put("settings", encodeSettings(db.settings))
        put("subscriptions", JsonArray(db.subscriptions.map(::encodeSubscription)))
    }

    fun encodeSettings(s: Settings): JsonObject = buildJsonObject {
        put("baseCurrency", s.baseCurrency)
        put("schemaVersion", s.schemaVersion)
        put("syncUrl", s.syncUrl)
        put("syncKey", s.syncKey)
        put("worthThreshold", number(s.worthThreshold))
        put("monthlyBudget", number(s.monthlyBudget))
        put("themeMode", s.themeMode.name)
    }

    fun encodeSubscription(s: Subscription): JsonObject = buildJsonObject {
        put("id", s.id)
        put("name", s.name)
        put(
            "cost",
            buildJsonObject {
                put("currency", s.cost.currency)
                put("minorUnits", s.cost.minorUnits.toString())
                put("exponent", s.cost.exponent)
            },
        )
        put("billingCycle", s.billingCycle.name)
        put("customDays", s.customDays)
        put("nextRenewal", s.nextRenewal)
        put("category", s.category)
        put("iconRef", s.iconRef)
        put("autoPay", s.autoPay)
        put("status", s.status.name)
        put("usesPerMonth", number(s.usesPerMonth))
        put("notes", s.notes)
        put("worthMode", s.worthMode.name)
        put("trialEnd", s.trialEnd)
        put("website", s.website)
        put("createdAt", s.createdAt)
        put("updatedAt", s.updatedAt)
        put("deletedAt", s.deletedAt)
    }

    fun decodeDatabase(obj: JsonObject): Database = Database(
        schemaVersion = string(obj.field("schemaVersion", "schema_version")),
        settings = decodeSettings(obj.objectOrNull("settings", "settings")),
        subscriptions = obj.arrayOrEmpty("subscriptions", "subscriptions").map { element ->
            decodeSubscription(element as? JsonObject ?: throw ProtoJsonException("a subscription is not an object"))
        },
    )

    fun decodeSettings(obj: JsonObject?): Settings {
        val o = obj ?: JsonObject(emptyMap())
        return Settings(
            baseCurrency = string(o.field("baseCurrency", "base_currency")),
            schemaVersion = string(o.field("schemaVersion", "schema_version")),
            syncUrl = string(o.field("syncUrl", "sync_url")),
            syncKey = string(o.field("syncKey", "sync_key")),
            worthThreshold = double(o.field("worthThreshold", "worth_threshold")),
            monthlyBudget = double(o.field("monthlyBudget", "monthly_budget")),
            themeMode = enum(o.field("themeMode", "theme_mode"), ThemeMode.entries, ThemeMode.SYSTEM),
        )
    }

    fun decodeSubscription(o: JsonObject): Subscription {
        val cost = o.objectOrNull("cost", "cost")
        return Subscription(
            id = string(o.field("id", "id")),
            name = string(o.field("name", "name")),
            cost = Money(
                currency = string(cost?.field("currency", "currency")),
                minorUnits = int64(cost?.field("minorUnits", "minor_units")),
                exponent = int32(cost?.field("exponent", "exponent")),
            ),
            billingCycle = enum(
                o.field("billingCycle", "billing_cycle"),
                BillingCycle.entries,
                BillingCycle.BILLING_CYCLE_UNSPECIFIED,
            ),
            customDays = int32(o.field("customDays", "custom_days")),
            nextRenewal = string(o.field("nextRenewal", "next_renewal")),
            category = string(o.field("category", "category")),
            iconRef = string(o.field("iconRef", "icon_ref")),
            autoPay = bool(o.field("autoPay", "auto_pay")),
            status = enum(o.field("status", "status"), SubStatus.entries, SubStatus.SUB_STATUS_UNSPECIFIED),
            usesPerMonth = double(o.field("usesPerMonth", "uses_per_month")),
            notes = string(o.field("notes", "notes")),
            worthMode = enum(o.field("worthMode", "worth_mode"), WorthMode.entries, WorthMode.AUTO),
            trialEnd = string(o.field("trialEnd", "trial_end")),
            website = string(o.field("website", "website")),
            createdAt = string(o.field("createdAt", "created_at")),
            updatedAt = string(o.field("updatedAt", "updated_at")),
            deletedAt = string(o.field("deletedAt", "deleted_at")),
        )
    }

    /** Whole doubles are written without a fraction, as the desktop's protobuf formatter does. */
    private fun number(value: Double): JsonPrimitive = when {
        value.isNaN() -> JsonPrimitive("NaN")
        value.isInfinite() -> JsonPrimitive(if (value > 0) "Infinity" else "-Infinity")
        value == Math.rint(value) && kotlin.math.abs(value) < MAX_EXACT_DOUBLE -> JsonPrimitive(value.toLong())
        else -> JsonPrimitive(value)
    }

    private fun JsonObject.field(jsonName: String, protoName: String): JsonElement? =
        (this[jsonName] ?: this[protoName])?.takeUnless { it is JsonNull }

    private fun JsonObject.objectOrNull(jsonName: String, protoName: String): JsonObject? =
        when (val value = field(jsonName, protoName)) {
            null -> null
            is JsonObject -> value
            else -> throw ProtoJsonException("$jsonName is not an object")
        }

    private fun JsonObject.arrayOrEmpty(jsonName: String, protoName: String): JsonArray =
        when (val value = field(jsonName, protoName)) {
            null -> JsonArray(emptyList())
            is JsonArray -> value
            else -> throw ProtoJsonException("$jsonName is not an array")
        }

    private fun primitive(value: JsonElement): JsonPrimitive =
        value as? JsonPrimitive ?: throw ProtoJsonException("expected a value, found $value")

    private fun string(value: JsonElement?): String {
        if (value == null) return ""
        val p = primitive(value)
        if (!p.isString) throw ProtoJsonException("expected text, found ${p.content}")
        return p.content
    }

    private fun bool(value: JsonElement?): Boolean {
        if (value == null) return false
        val p = primitive(value)
        if (p.isString || (p.content != "true" && p.content != "false")) {
            throw ProtoJsonException("expected true or false, found ${p.content}")
        }
        return p.content == "true"
    }

    private fun integral(value: JsonElement): BigDecimal {
        val text = primitive(value).content
        val number = text.toBigDecimalOrNull() ?: throw ProtoJsonException("expected an integer, found $text")
        if (number.stripTrailingZeros().scale() > 0) throw ProtoJsonException("expected an integer, found $text")
        return number
    }

    private fun int64(value: JsonElement?): Long =
        if (value == null) {
            0
        } else {
            runCatching { integral(value).longValueExact() }
                .getOrElse { throw ProtoJsonException("int64 out of range: $value") }
        }

    private fun int32(value: JsonElement?): Int =
        if (value == null) {
            0
        } else {
            runCatching { integral(value).intValueExact() }
                .getOrElse { throw ProtoJsonException("int32 out of range: $value") }
        }

    private fun double(value: JsonElement?): Double {
        if (value == null) return 0.0
        val p = primitive(value)
        return when (p.content) {
            "NaN" -> Double.NaN
            "Infinity" -> Double.POSITIVE_INFINITY
            "-Infinity" -> Double.NEGATIVE_INFINITY
            else -> p.content.toDoubleOrNull() ?: throw ProtoJsonException("expected a number, found ${p.content}")
        }
    }

    /** Names or proto numbers; an unknown value is ignored like an unknown field. */
    private fun <E : Enum<E>> enum(value: JsonElement?, entries: List<E>, default: E): E {
        if (value == null) return default
        val p = primitive(value)
        return if (p.isString) {
            entries.firstOrNull { it.name == p.content } ?: default
        } else {
            p.content.toIntOrNull()?.let(entries::getOrNull) ?: default
        }
    }
}
