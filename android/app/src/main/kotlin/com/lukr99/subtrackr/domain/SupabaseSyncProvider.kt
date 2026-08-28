package com.lukr99.subtrackr.domain

import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.WorthMode
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import okhttp3.Headers
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody

/**
 * Sync transport backed by Supabase (PostgREST) using a real relational `subscriptions` table
 * (one row per subscription, typed columns). Pull selects all rows, push bulk-upserts by id;
 * deletes are soft (deleted_at). Mirrors the WPF SupabaseSyncProvider. See docs/SYNC-SETUP.md.
 */
class SupabaseSyncProvider(
    projectUrl: String,
    private val anonKey: String,
    private val docId: String = "main",
    private val client: OkHttpClient = OkHttpClient(),
) : SyncProvider {

    private val base = projectUrl.trimEnd('/')
    private val json = Json { ignoreUnknownKeys = true; encodeDefaults = true }

    @Serializable
    private data class Row(
        val id: String,
        val name: String = "",
        @SerialName("cost_currency") val costCurrency: String = "EUR",
        @SerialName("cost_minor") val costMinor: Long = 0,
        @SerialName("cost_exponent") val costExponent: Int = 2,
        @SerialName("billing_cycle") val billingCycle: String = "MONTHLY",
        @SerialName("custom_days") val customDays: Int = 0,
        @SerialName("next_renewal") val nextRenewal: String = "",
        val category: String = "",
        @SerialName("icon_ref") val iconRef: String = "",
        @SerialName("auto_pay") val autoPay: Boolean = false,
        val status: String = "ACTIVE",
        @SerialName("uses_per_month") val usesPerMonth: Double = 0.0,
        @SerialName("worth_mode") val worthMode: String = "AUTO",
        @SerialName("trial_end") val trialEnd: String = "",
        val website: String = "",
        val notes: String = "",
        @SerialName("created_at") val createdAt: String = "",
        @SerialName("updated_at") val updatedAt: String = "",
        @SerialName("deleted_at") val deletedAt: String = "",
    )

    private fun headers() = Headers.Builder()
        .add("apikey", anonKey)
        .add("Authorization", "Bearer $anonKey")
        .build()

    override suspend fun pull(): List<Subscription> = withContext(Dispatchers.IO) {
        val req = Request.Builder()
            .url("$base/rest/v1/subscriptions?select=*")
            .headers(headers()).get().build()
        client.newCall(req).execute().use { resp ->
            if (!resp.isSuccessful) throw RuntimeException("pull HTTP ${resp.code}")
            val body = resp.body?.string() ?: "[]"
            json.decodeFromString<List<Row>>(body).map { it.toSub() }
        }
    }

    override suspend fun push(subscriptions: List<Subscription>) {
        if (subscriptions.isEmpty()) return
        withContext(Dispatchers.IO) {
            val payload = json.encodeToString(rowListSerializer, subscriptions.map { it.toRow() })
            val req = Request.Builder()
                .url("$base/rest/v1/subscriptions")
                .headers(headers().newBuilder().add("Prefer", "resolution=merge-duplicates,return=minimal").build())
                .post(payload.toRequestBody("application/json".toMediaType()))
                .build()
            client.newCall(req).execute().use { resp ->
                if (!resp.isSuccessful) throw RuntimeException("push HTTP ${resp.code}")
            }
        }
    }

    private val rowListSerializer = kotlinx.serialization.builtins.ListSerializer(Row.serializer())

    private fun Subscription.toRow() = Row(
        id = id, name = name,
        costCurrency = cost.currency, costMinor = cost.minorUnits, costExponent = cost.exponent,
        billingCycle = billingCycle.name, customDays = customDays, nextRenewal = nextRenewal,
        category = category, iconRef = iconRef, autoPay = autoPay, status = status.name,
        usesPerMonth = usesPerMonth, worthMode = worthMode.name, trialEnd = trialEnd,
        website = website, notes = notes, createdAt = createdAt, updatedAt = updatedAt, deletedAt = deletedAt,
    )

    private fun Row.toSub() = Subscription(
        id = id, name = name,
        cost = Money(costCurrency, costMinor, costExponent),
        billingCycle = runCatching { BillingCycle.valueOf(billingCycle) }.getOrDefault(BillingCycle.MONTHLY),
        customDays = customDays, nextRenewal = nextRenewal, category = category, iconRef = iconRef,
        autoPay = autoPay,
        status = runCatching { SubStatus.valueOf(status) }.getOrDefault(SubStatus.ACTIVE),
        usesPerMonth = usesPerMonth,
        worthMode = runCatching { WorthMode.valueOf(worthMode) }.getOrDefault(WorthMode.AUTO),
        trialEnd = trialEnd, website = website, notes = notes,
        createdAt = createdAt, updatedAt = updatedAt, deletedAt = deletedAt,
    )
}
