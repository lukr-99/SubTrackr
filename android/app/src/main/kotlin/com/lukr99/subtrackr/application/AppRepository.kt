package com.lukr99.subtrackr.application

import com.lukr99.subtrackr.application.rates.ExchangeRateSource
import com.lukr99.subtrackr.application.store.DatabaseStore
import com.lukr99.subtrackr.application.sync.SyncProviderFactory
import com.lukr99.subtrackr.application.sync.SyncService
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.domain.spend.SpendCalculator
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.domain.worth.WorthIt
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.ThemeMode
import java.math.BigDecimal
import java.time.Clock
import java.time.LocalDate

/**
 * In-memory app state backed by a [DatabaseStore] (mirrors the desktop AppState). Every change is
 * saved at once. Clock and ID generation are injected so tests are deterministic.
 */
class AppRepository(
    private val store: DatabaseStore,
    private val rateSource: ExchangeRateSource,
    private val syncProviders: SyncProviderFactory,
    private val clock: Clock,
    private val newId: () -> String,
) {
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

    /** Today in the clock's time zone, for renewal and trial reminders. */
    fun today(): LocalDate = LocalDate.now(clock)

    fun summarize(): SpendSummary =
        SpendCalculator.summarize(db.subscriptions, baseCurrency, rates, worthThreshold)

    fun upsert(sub: Subscription) {
        val now = clock.instant().toString()
        val list = db.subscriptions.toMutableList()
        val idx = list.indexOfFirst { it.id == sub.id && sub.id.isNotEmpty() }
        val finalSub = sub.copy(
            id = sub.id.ifEmpty { newId() },
            updatedAt = now,
            createdAt = if (idx >= 0) list[idx].createdAt else now,
        )
        if (idx >= 0) list[idx] = finalSub else list.add(finalSub)
        save(db.copy(subscriptions = list))
    }

    fun delete(id: String) {
        val now = clock.instant().toString()
        val list = db.subscriptions.map {
            if (it.id == id) it.copy(deletedAt = now, updatedAt = now) else it
        }
        save(db.copy(subscriptions = list))
    }

    fun setBaseCurrency(currency: String) {
        save(db.copy(settings = db.settings.copy(baseCurrency = currency.uppercase())))
        rates = OfflineFallback.forAnchor(baseCurrency)
    }

    fun setWorthThreshold(threshold: BigDecimal) {
        save(db.copy(settings = db.settings.copy(worthThreshold = threshold.toDouble())))
    }

    val monthlyBudget: BigDecimal get() = BigDecimal(db.settings.monthlyBudget.toString())

    fun setMonthlyBudget(budget: BigDecimal) {
        save(db.copy(settings = db.settings.copy(monthlyBudget = budget.toDouble())))
    }

    val themeMode: ThemeMode get() = db.settings.themeMode

    fun setThemeMode(mode: ThemeMode) {
        save(db.copy(settings = db.settings.copy(themeMode = mode)))
    }

    suspend fun refreshRates() {
        rates = rateSource.latest(baseCurrency)
    }

    // ----- sync (device-local config; only subscriptions sync) -----

    val syncUrl: String get() = db.settings.syncUrl
    val syncKey: String get() = db.settings.syncKey
    val syncConfigured: Boolean get() = syncUrl.isNotBlank() && syncKey.isNotBlank()

    fun setSyncConfig(url: String, key: String) {
        save(db.copy(settings = db.settings.copy(syncUrl = url.trim(), syncKey = key.trim())))
    }

    suspend fun syncNow(): Int {
        val merged = SyncService.sync(db.subscriptions, syncProviders.create(syncUrl, syncKey))
        save(db.copy(subscriptions = merged))
        return merged.size
    }

    private fun save(next: Database) {
        store.save(next)
        db = next
    }
}
