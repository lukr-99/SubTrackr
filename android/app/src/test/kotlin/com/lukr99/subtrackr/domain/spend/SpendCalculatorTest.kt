package com.lukr99.subtrackr.domain.spend

import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import org.junit.Assert.assertEquals
import org.junit.Test
import java.math.BigDecimal

class SpendCalculatorTest {
    private val rates = OfflineFallback.forAnchor("EUR")

    private fun summarize(vararg subs: Subscription) =
        SpendCalculator.summarize(subs.toList(), "EUR", rates, BigDecimal("1.50"))

    @Test
    fun summarize_excludesPausedAndDeletedFromActiveSpend() {
        val summary = summarize(
            Subscription(id = "a", cost = Money("EUR", 1000, 2)),
            Subscription(id = "b", cost = Money("EUR", 500, 2), status = SubStatus.PAUSED),
            Subscription(id = "c", cost = Money("EUR", 700, 2), deletedAt = "2026-09-01T10:00:00Z"),
        )

        assertEquals(0, BigDecimal("10").compareTo(summary.monthlyBase))
        assertEquals(2, summary.perSub.size)
    }

    @Test
    fun summarize_unspecifiedOrZeroDayCycles_countAsMonthlyInsteadOfFailing() {
        val summary = summarize(
            Subscription(id = "a", cost = Money("EUR", 1000, 2), billingCycle = BillingCycle.BILLING_CYCLE_UNSPECIFIED),
            Subscription(id = "b", cost = Money("EUR", 500, 2), billingCycle = BillingCycle.CUSTOM_DAYS, customDays = 0),
            Subscription(id = "c", cost = Money("", 0, 0), status = SubStatus.SUB_STATUS_UNSPECIFIED),
        )

        assertEquals(0, BigDecimal("15").compareTo(summary.monthlyBase))
    }
}
