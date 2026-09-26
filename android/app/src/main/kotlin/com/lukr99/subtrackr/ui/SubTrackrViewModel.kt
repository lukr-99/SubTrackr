package com.lukr99.subtrackr.ui

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.ThemeMode
import kotlinx.coroutines.launch
import java.math.BigDecimal

/** Dashboard, editor, what-if, and settings state over the injected [AppRepository]. */
class SubTrackrViewModel(private val repo: AppRepository) : ViewModel() {

    var summary by mutableStateOf(repo.summarize())
        private set
    var baseCurrency by mutableStateOf(repo.baseCurrency)
        private set
    var ratesLabel by mutableStateOf(ratesText())
        private set
    var themeMode by mutableStateOf(repo.themeMode)
        private set

    init {
        // Subscription sync is independent of exchange rates and must still run if that endpoint
        // is slow or offline.
        autoSync()
        viewModelScope.launch {
            repo.refreshRates()
            refresh()
        }
    }

    /** Best-effort silent sync when configured (launch + after every change). */
    private fun autoSync() {
        if (!repo.syncConfigured) return
        viewModelScope.launch {
            try {
                repo.syncNow()
                refresh()
            } catch (_: Exception) {
            }
        }
    }

    val worthThreshold: BigDecimal get() = repo.worthThreshold
    val rates: ExchangeRateTable get() = repo.rates

    private fun refresh() {
        summary = repo.summarize()
        baseCurrency = repo.baseCurrency
        ratesLabel = ratesText()
        themeMode = repo.themeMode
    }

    fun changeThemeMode(mode: ThemeMode) {
        repo.setThemeMode(mode)
        refresh()
    }

    private fun ratesText() = "${repo.rates.anchor} · ${repo.rates.date}"

    fun upsert(sub: Subscription) {
        repo.upsert(sub)
        refresh()
        autoSync()
    }

    fun delete(id: String) {
        repo.delete(id)
        refresh()
        autoSync()
    }

    fun changeBaseCurrency(currency: String) {
        repo.setBaseCurrency(currency)
        refresh()
        viewModelScope.launch {
            repo.refreshRates()
            refresh()
        }
    }

    fun setWorthThreshold(threshold: BigDecimal) {
        repo.setWorthThreshold(threshold)
        refresh()
    }

    val monthlyBudget: BigDecimal get() = repo.monthlyBudget

    fun setMonthlyBudget(budget: BigDecimal) {
        repo.setMonthlyBudget(budget)
        refresh()
    }

    fun refreshRates() = viewModelScope.launch {
        repo.refreshRates()
        refresh()
    }

    val syncUrl: String get() = repo.syncUrl
    val syncKey: String get() = repo.syncKey

    fun saveSyncConfig(url: String, key: String) = repo.setSyncConfig(url, key)

    fun syncNow(onResult: (String) -> Unit) {
        if (!repo.syncConfigured) {
            onResult("Enter the URL and key first.")
            return
        }
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
}
