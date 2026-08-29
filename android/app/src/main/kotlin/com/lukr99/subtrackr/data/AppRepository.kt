package com.lukr99.subtrackr.data

import com.lukr99.subtrackr.domain.ExchangeRateTable
import com.lukr99.subtrackr.domain.FrankfurterRateProvider
import com.lukr99.subtrackr.domain.OfflineFallback
import com.lukr99.subtrackr.domain.SpendCalculator
import com.lukr99.subtrackr.domain.SpendSummary
import com.lukr99.subtrackr.domain.SupabaseSyncProvider
import com.lukr99.subtrackr.domain.SyncService
import com.lukr99.subtrackr.domain.WorthIt
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Subscription
import java.io.File
import java.math.BigDecimal
import java.time.Instant
import java.util.UUID

/** In-memory app state backed by the JSON store (mirrors the WPF AppState). */
class AppRepository(file: File) {
    private val store = SubscriptionStore(file)

    var db: Database = store.load()
        private set
    var rates: ExchangeRateTable = OfflineFallback.forAnchor(db.settings.baseCurrency)
        private set
    /** Cost-per-use cutoff in base currency; stored 0 = currency-aware default. */
    val worthThreshold: BigDecimal
        get() {
            val stored = db.settings.worthThreshold
            return if (stored > 0.0) BigDecimal(stored.toString()) else WorthIt.defaultThresholdFor(baseCurrency)
        }

    val baseCurrency: String get() = db.settings.baseCurrency.ifBlank { "EUR" }

    fun summarize(): SpendSummary =
        SpendCalculator.summarize(db.subscriptions, baseCurrency, rates, worthThreshold)

    fun upsert(sub: Subscription) {
        val now = Instant.now().toString()
        val list = db.subscriptions.toMutableList()
        val idx = list.indexOfFirst { it.id == sub.id && sub.id.isNotEmpty() }
        val finalSub = sub.copy(
            id = sub.id.ifEmpty { UUID.randomUUID().toString() },
            updatedAt = now,
            createdAt = if (idx >= 0) list[idx].createdAt else now,
        )
        if (idx >= 0) list[idx] = finalSub else list.add(finalSub)
        db = db.copy(subscriptions = list)
        store.save(db)
    }

    fun delete(id: String) {
        val now = Instant.now().toString()
        val list = db.subscriptions.map {
            if (it.id == id) it.copy(deletedAt = now, updatedAt = now) else it
        }
        db = db.copy(subscriptions = list)
        store.save(db)
    }

    fun setBaseCurrency(currency: String) {
        db = db.copy(settings = db.settings.copy(baseCurrency = currency.uppercase()))
        store.save(db)
        rates = OfflineFallback.forAnchor(baseCurrency)
    }

    fun setWorthThreshold(threshold: BigDecimal) {
        db = db.copy(settings = db.settings.copy(worthThreshold = threshold.toDouble()))
        store.save(db)
    }

    suspend fun refreshRates(provider: FrankfurterRateProvider) {
        rates = provider.getRates(baseCurrency)
    }

    // ----- sync (device-local config; only subscriptions sync) -----

    val syncUrl: String get() = db.settings.syncUrl
    val syncKey: String get() = db.settings.syncKey
    val syncConfigured: Boolean get() = syncUrl.isNotBlank() && syncKey.isNotBlank()

    fun setSyncConfig(url: String, key: String) {
        db = db.copy(settings = db.settings.copy(syncUrl = url.trim(), syncKey = key.trim()))
        store.save(db)
    }

    suspend fun syncNow(): Int {
        val provider = SupabaseSyncProvider(syncUrl, syncKey)
        val merged = SyncService.sync(db.subscriptions, provider)
        db = db.copy(subscriptions = merged)
        store.save(db)
        return merged.size
    }
}
