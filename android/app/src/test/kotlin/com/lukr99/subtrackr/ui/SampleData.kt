package com.lukr99.subtrackr.ui

import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.domain.spend.SpendCalculator
import com.lukr99.subtrackr.domain.spend.SpendSummary
import com.lukr99.subtrackr.domain.worth.WorthIt
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.WorthMode
import java.math.BigDecimal
import java.time.Clock
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneOffset

/** Generic, fixed data for screenshots: no real accounts, no logos to fetch, a fixed "today". */
object SampleData {
    val today: LocalDate = LocalDate.of(2026, 9, 26)
    val clock: Clock = Clock.fixed(Instant.parse("2026-09-26T10:00:00Z"), ZoneOffset.UTC)
    const val BASE = "CZK"
    val rates: ExchangeRateTable = OfflineFallback.forAnchor(BASE)
    val threshold: BigDecimal = WorthIt.defaultThresholdFor(BASE)

    private fun sub(
        id: Int,
        name: String,
        amount: String,
        currency: String,
        category: String,
        icon: String,
        renewal: String,
        uses: Double,
        cycle: BillingCycle = BillingCycle.MONTHLY,
        status: SubStatus = SubStatus.ACTIVE,
        worth: WorthMode = WorthMode.AUTO,
        trialEnd: String = "",
    ) = Subscription(
        id = "00000000-0000-4000-8000-%012d".format(id),
        name = name,
        cost = Money.of(BigDecimal(amount), currency),
        billingCycle = cycle,
        nextRenewal = renewal,
        category = category,
        iconRef = icon,
        autoPay = true,
        status = status,
        usesPerMonth = uses,
        worthMode = worth,
        trialEnd = trialEnd,
        createdAt = "2026-09-01T08:00:00Z",
        updatedAt = "2026-09-01T08:00:00Z",
    )

    val subscriptions: List<Subscription> = listOf(
        sub(1, "Video streaming", "199", "CZK", "Entertainment", "🎬", "2026-09-28", 8.0),
        sub(2, "AI assistant", "20", "USD", "Productivity", "🤖", "2026-10-02", 40.0),
        sub(3, "Music", "10.99", "EUR", "Music", "🎧", "2026-10-12", 90.0),
        sub(4, "Mobile plan", "450", "CZK", "Phone & Internet", "📱", "2026-10-06", 0.0, worth = WorthMode.ESSENTIAL),
        sub(5, "Meal kit", "149", "CZK", "Food", "🍔", "2026-10-19", 2.0, trialEnd = "2026-09-30"),
        sub(6, "City transit pass", "2400", "CZK", "Transport", "🚊", "2027-03-10", 40.0, cycle = BillingCycle.ANNUAL),
        sub(7, "Cloud storage", "2.99", "EUR", "Productivity", "☁️", "2026-10-20", 0.0, status = SubStatus.PAUSED),
    )

    val database: Database = Database(
        settings = Settings(baseCurrency = BASE, monthlyBudget = 1500.0),
        subscriptions = subscriptions,
    )

    val summary: SpendSummary = SpendCalculator.summarize(subscriptions, BASE, rates, threshold)
    val budget: BigDecimal = BigDecimal("1500")
}
