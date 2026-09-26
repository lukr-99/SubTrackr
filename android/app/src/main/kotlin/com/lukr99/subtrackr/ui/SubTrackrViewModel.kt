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
import java.time.LocalDate

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
        // Restores and sync passes change the repository too; the screens follow every change.
        viewModelScope.launch { repo.database.collect { refresh() } }
        viewModelScope.launch {
            repo.refreshRates()
            refresh()
        }
    }

    val worthThreshold: BigDecimal get() = repo.worthThreshold
    val today: LocalDate get() = repo.today()
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
    }

    fun delete(id: String) {
        repo.delete(id)
        refresh()
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
}
