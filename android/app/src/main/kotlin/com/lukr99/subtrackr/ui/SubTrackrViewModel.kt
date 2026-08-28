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

    fun upsert(sub: Subscription) { repo.upsert(sub); refresh() }
    fun delete(id: String) { repo.delete(id); refresh() }

    fun changeBaseCurrency(currency: String) {
        repo.setBaseCurrency(currency)
        refresh()
        viewModelScope.launch { repo.refreshRates(rateProvider); refresh() }
    }

    fun setWorthThreshold(threshold: BigDecimal) { repo.setWorthThreshold(threshold); refresh() }

    fun refreshRates() = viewModelScope.launch { repo.refreshRates(rateProvider); refresh() }

    /** Current summary of a hypothetical portfolio (existing actives + one extra sub). */
    fun summaryWith(extra: Subscription): SpendSummary {
        val hypothetical = repo.db.subscriptions + extra
        return com.lukr99.subtrackr.domain.SpendCalculator.summarize(
            hypothetical, baseCurrency, repo.rates, repo.worthThreshold,
        )
    }
}
