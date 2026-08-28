package com.lukr99.subtrackr.ui

import android.app.Application
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.lukr99.subtrackr.data.AppRepository
import com.lukr99.subtrackr.domain.ExchangeRateTable
import com.lukr99.subtrackr.domain.FrankfurterRateProvider
import com.lukr99.subtrackr.domain.SpendSummary
import com.lukr99.subtrackr.model.Subscription
import kotlinx.coroutines.launch
import java.io.File
import java.math.BigDecimal

class SubTrackrViewModel(app: Application) : AndroidViewModel(app) {
    private val repo = AppRepository(File(app.filesDir, "data.json"))
    private val rateProvider = FrankfurterRateProvider()

    var summary by mutableStateOf(repo.summarize())
        private set
    var baseCurrency by mutableStateOf(repo.baseCurrency)
        private set
    var ratesLabel by mutableStateOf(ratesText())
        private set

    init {
        viewModelScope.launch {
            repo.refreshRates(rateProvider)
            refresh()
            autoSync()
        }
    }

    /** Best-effort silent sync when configured (launch + after every change). */
    private fun autoSync() {
        if (!repo.syncConfigured) return
        viewModelScope.launch {
            try { repo.syncNow(); refresh() } catch (_: Exception) {}
        }
    }

    val worthThreshold: BigDecimal get() = repo.worthThreshold
    val rates: ExchangeRateTable get() = repo.rates

    private fun refresh() {
        summary = repo.summarize()
        baseCurrency = repo.baseCurrency
        ratesLabel = ratesText()
    }

    private fun ratesText() = "${repo.rates.anchor} · ${repo.rates.date}"

    fun upsert(sub: Subscription) { repo.upsert(sub); refresh(); autoSync() }
    fun delete(id: String) { repo.delete(id); refresh(); autoSync() }

    fun changeBaseCurrency(currency: String) {
        repo.setBaseCurrency(currency)
        refresh()
        viewModelScope.launch { repo.refreshRates(rateProvider); refresh() }
    }

    fun setWorthThreshold(threshold: BigDecimal) { repo.setWorthThreshold(threshold); refresh() }

    fun refreshRates() = viewModelScope.launch { repo.refreshRates(rateProvider); refresh() }

    val syncUrl: String get() = repo.syncUrl
    val syncKey: String get() = repo.syncKey

    fun saveSyncConfig(url: String, key: String) = repo.setSyncConfig(url, key)

    fun syncNow(onResult: (String) -> Unit) {
        if (!repo.syncConfigured) { onResult("Enter the URL and key first."); return }
        viewModelScope.launch {
            try {
                val n = repo.syncNow()
                refresh()
                onResult("Synced · $n items")
            } catch (e: Exception) {
                onResult("Sync failed — check URL/key and connection.")
            }
        }
    }

    /** Current summary of a hypothetical portfolio (existing actives + one extra sub). */
    fun summaryWith(extra: Subscription): SpendSummary {
        val hypothetical = repo.db.subscriptions + extra
        return com.lukr99.subtrackr.domain.SpendCalculator.summarize(
            hypothetical, baseCurrency, repo.rates, repo.worthThreshold,
        )
    }
}
