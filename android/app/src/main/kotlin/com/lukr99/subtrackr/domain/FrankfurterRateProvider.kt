package com.lukr99.subtrackr.domain

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import org.json.JSONObject
import java.math.BigDecimal
import java.util.concurrent.TimeUnit

/**
 * Live FX rates from the Frankfurter API (ECB data, no key), falling back to [OfflineFallback]
 * on any failure. Mirrors the WPF FrankfurterRateProvider. See SPEC.md §3.
 */
class FrankfurterRateProvider(
    private val client: OkHttpClient = OkHttpClient.Builder()
        .callTimeout(8, TimeUnit.SECONDS)
        .build(),
) {
    suspend fun getRates(anchor: String): ExchangeRateTable = withContext(Dispatchers.IO) {
        val a = anchor.uppercase()
        try {
            val request = Request.Builder()
                .url("https://api.frankfurter.dev/v1/latest?base=$a")
                .header("User-Agent", "subtrackr-android")
                .build()
            client.newCall(request).execute().use { resp ->
                if (!resp.isSuccessful) return@withContext OfflineFallback.forAnchor(a)
                val body = resp.body?.string() ?: return@withContext OfflineFallback.forAnchor(a)
                val json = JSONObject(body)
                val date = json.optString("date", "live")
                val ratesObj = json.getJSONObject("rates")
                val map = HashMap<String, BigDecimal>()
                for (key in ratesObj.keys()) map[key] = BigDecimal(ratesObj.get(key).toString())
                ExchangeRateTable(a, map, date)
            }
        } catch (e: Exception) {
            OfflineFallback.forAnchor(a)
        }
    }
}
