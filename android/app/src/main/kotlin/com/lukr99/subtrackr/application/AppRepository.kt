package com.lukr99.subtrackr.application

import com.lukr99.subtrackr.application.rates.ExchangeRateSource
import com.lukr99.subtrackr.application.store.DatabaseStore
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.domain.spend.SpendCalculator
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.domain.sync.MergeEngine
import com.lukr99.subtrackr.domain.worth.WorthIt
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.ThemeMode
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import java.math.BigDecimal
import java.time.Clock
import java.time.LocalDate

/**
 * In-memory app state backed by a [DatabaseStore] (mirrors the desktop AppState). Every change is
 * saved at once, under one lock, so edits, restores, and sync passes never lose each other's work.
 * Clock and ID generation are injected so tests are deterministic.
 */
class AppRepository(
    private val store: DatabaseStore,
    private val rateSource: ExchangeRateSource,
    private val clock: Clock,
    private val newId: () -> String,
) {
    private val lock = Any()
    private val state = MutableStateFlow(store.load())

    private val subscriptionEdits = MutableSharedFlow<Unit>(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    /** The current database; it changes only after the store saved the new version. */
    val database: StateFlow<Database> = state.asStateFlow()
    val db: Database get() = state.value

    /** Emits after subscriptions changed on this device (edits, restores), not after a sync merge. */
    val localChanges: SharedFlow<Unit> = subscriptionEdits.asSharedFlow()

    @Volatile
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

    /**
     * Applies [transform] to the current database and saves the result as one atomic step. If the
     * save fails, nothing changes and the exception propagates.
     */
    fun <T> update(transform: (Database) -> Pair<Database, T>): T = update(local = true, transform)

    private fun <T> update(local: Boolean, transform: (Database) -> Pair<Database, T>): T = synchronized(lock) {
        val previous = state.value
        val (next, result) = transform(previous)
        if (next != previous) {
            store.save(next)
            state.value = next
            if (local && next.subscriptions != previous.subscriptions) subscriptionEdits.tryEmit(Unit)
        }
        if (next.settings.baseCurrency != rates.anchor && next.settings.baseCurrency.isNotBlank()) {
            rates = OfflineFallback.forAnchor(baseCurrency)
        }
        result
    }

    private fun change(transform: (Database) -> Database) = update { transform(it) to Unit }

    fun upsert(sub: Subscription) = change { db ->
        val now = clock.instant().toString()
        val list = db.subscriptions.toMutableList()
        val idx = list.indexOfFirst { it.id == sub.id && sub.id.isNotEmpty() }
        val finalSub = sub.copy(
            id = sub.id.ifEmpty { newId() },
            updatedAt = now,
            createdAt = if (idx >= 0) list[idx].createdAt else now,
        )
        if (idx >= 0) list[idx] = finalSub else list.add(finalSub)
        db.copy(subscriptions = list)
    }

    fun delete(id: String) = change { db ->
        val now = clock.instant().toString()
        db.copy(subscriptions = db.subscriptions.map { if (it.id == id) it.copy(deletedAt = now, updatedAt = now) else it })
    }

    fun setBaseCurrency(currency: String) = change { it.copy(settings = it.settings.copy(baseCurrency = currency.uppercase())) }

    fun setWorthThreshold(threshold: BigDecimal) =
        change { it.copy(settings = it.settings.copy(worthThreshold = threshold.toDouble())) }

    val monthlyBudget: BigDecimal get() = BigDecimal(db.settings.monthlyBudget.toString())

    fun setMonthlyBudget(budget: BigDecimal) =
        change { it.copy(settings = it.settings.copy(monthlyBudget = budget.toDouble())) }

    val themeMode: ThemeMode get() = db.settings.themeMode

    fun setThemeMode(mode: ThemeMode) = change { it.copy(settings = it.settings.copy(themeMode = mode)) }

    val hideServiceLogos: Boolean get() = db.settings.hideServiceLogos

    fun setHideServiceLogos(hide: Boolean) = change { it.copy(settings = it.settings.copy(hideServiceLogos = hide)) }

    suspend fun refreshRates() {
        rates = rateSource.latest(baseCurrency)
    }

    // ----- sync configuration (device-local; only subscriptions sync) -----

    val syncUrl: String get() = db.settings.syncUrl
    val syncKey: String get() = db.settings.syncKey

    fun setSyncConfig(url: String, key: String) =
        change { it.copy(settings = it.settings.copy(syncUrl = url.trim(), syncKey = key.trim())) }

    /**
     * Merges pulled rows into the current subscriptions (SPEC.md section 8.1) and saves the result,
     * which is also the set to push. Edits made while the pull ran are part of the merge.
     */
    fun mergeRemote(remote: List<Subscription>): List<Subscription> = update(local = false) { current ->
        val merged = MergeEngine.merge(current.subscriptions, remote)
        current.copy(subscriptions = merged) to merged
    }
}
