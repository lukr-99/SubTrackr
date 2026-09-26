package com.lukr99.subtrackr.data.rates

import com.lukr99.subtrackr.application.rates.ExchangeRateSource
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import okhttp3.OkHttpClient
import okhttp3.Request
import java.math.BigDecimal

/**
 * Live FX rates from the Frankfurter API (ECB data, no key), falling back to [OfflineFallback]
 * on any failure. Mirrors the desktop FrankfurterRateProvider. See SPEC.md section 3.
 */
class FrankfurterRateSource(
    private val client: OkHttpClient,
    private val baseUrl: String = "https://api.frankfurter.dev",
) : ExchangeRateSource {

    override suspend fun latest(anchor: String): ExchangeRateTable = withContext(Dispatchers.IO) {
        val a = anchor.uppercase()
        try {
            val request = Request.Builder()
                .url("$baseUrl/v1/latest?base=$a")
                .header("User-Agent", "subtrackr-android")
                .build()
            client.newCall(request).execute().use { resp ->
                val body = resp.body?.string()
                if (!resp.isSuccessful || body == null) return@withContext OfflineFallback.forAnchor(a)
                val json = Json.parseToJsonElement(body).jsonObject
                val date = json["date"]?.jsonPrimitive?.content ?: "live"
                val rates = json.getValue("rates").jsonObject
                    .mapValues { (_, value) -> BigDecimal(value.jsonPrimitive.content) }
                ExchangeRateTable(a, rates, date)
            }
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (_: Exception) {
            OfflineFallback.forAnchor(a)
        }
    }
}
