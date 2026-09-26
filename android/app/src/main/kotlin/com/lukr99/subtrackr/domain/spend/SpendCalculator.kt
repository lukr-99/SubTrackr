package com.lukr99.subtrackr.domain.spend

import com.lukr99.subtrackr.domain.currency.ExchangeRateTable
import com.lukr99.subtrackr.domain.worth.WorthIt
import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.Subscription
import java.math.BigDecimal

/** Portfolio rollup (SPEC.md section 4). Active-spend totals exclude PAUSED and soft-deleted records. */
object SpendCalculator {
    fun summarize(
        subscriptions: List<Subscription>,
        baseCurrency: String,
        rates: ExchangeRateTable,
        worthThreshold: BigDecimal,
    ): SpendSummary {
        val base = baseCurrency.uppercase()
        val perSub = subscriptions
            .filter { it.deletedAt.isEmpty() }
            .map { s ->
                val monthlyOwn = monthlyOwn(s)
                val monthlyBase = if (rates.knows(s.cost.currency)) {
                    rates.convert(monthlyOwn, s.cost.currency, base)
                } else {
                    monthlyOwn
                }
                SubscriptionSpend(
                    subscription = s,
                    monthlyOwn = monthlyOwn,
                    monthlyBase = monthlyBase,
                    yearlyBase = monthlyBase.multiply(BigDecimal(12)),
                    costPerUse = WorthIt.costPerUse(monthlyBase, s.usesPerMonth),
                    verdict = WorthIt.evaluate(monthlyBase, s.usesPerMonth, worthThreshold, s.worthMode),
                )
            }

        val active = perSub.filter { it.subscription.status != SubStatus.PAUSED }
        val monthlyTotal = active.fold(BigDecimal.ZERO) { acc, p -> acc + p.monthlyBase }

        val perCurrency = active
            .groupBy { it.subscription.cost.currency.uppercase() }
            .map { (ccy, items) ->
                val m = items.fold(BigDecimal.ZERO) { a, p -> a + p.monthlyOwn }
                CurrencySubtotal(ccy, m, m.multiply(BigDecimal(12)))
            }
            .sortedByDescending { it.monthly }

        val byCategory = active
            .groupBy { it.subscription.category.ifBlank { "Uncategorized" } }
            .map { (cat, items) ->
                val m = items.fold(BigDecimal.ZERO) { a, p -> a + p.monthlyBase }
                CategorySlice(cat, m, m.multiply(BigDecimal(12)))
            }
            .sortedByDescending { it.monthlyBase }

        return SpendSummary(base, monthlyTotal, monthlyTotal.multiply(BigDecimal(12)), perSub, perCurrency, byCategory)
    }

    /**
     * Records from a backup, a sync row, or another app can carry an unspecified cycle or a custom
     * cycle of zero days. They count as monthly so one odd record cannot break every total.
     */
    private fun monthlyOwn(s: Subscription): BigDecimal {
        val cycle = when {
            s.billingCycle == BillingCycle.BILLING_CYCLE_UNSPECIFIED -> BillingCycle.MONTHLY
            s.billingCycle == BillingCycle.CUSTOM_DAYS && s.customDays <= 0 -> BillingCycle.MONTHLY
            else -> s.billingCycle
        }
        return Normalization.monthlyEquivalent(s.cost.toBigDecimal(), cycle, s.customDays)
    }
}
