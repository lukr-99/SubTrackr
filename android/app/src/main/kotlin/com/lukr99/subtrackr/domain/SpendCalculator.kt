package com.lukr99.subtrackr.domain

import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.model.SubStatus
import java.math.BigDecimal

data class SubscriptionSpend(
    val subscription: Subscription,
    val monthlyOwn: BigDecimal,
    val monthlyBase: BigDecimal,
    val yearlyBase: BigDecimal,
    val costPerUse: BigDecimal,
    val verdict: WorthVerdict,
)

data class CurrencySubtotal(val currency: String, val monthly: BigDecimal, val yearly: BigDecimal)
data class CategorySlice(val category: String, val monthlyBase: BigDecimal, val yearlyBase: BigDecimal)

data class SpendSummary(
    val baseCurrency: String,
    val monthlyBase: BigDecimal,
    val yearlyBase: BigDecimal,
    val perSub: List<SubscriptionSpend>,
    val perCurrency: List<CurrencySubtotal>,
    val byCategory: List<CategorySlice>,
)

/** Portfolio rollup (SPEC.md §4). Active-spend totals exclude PAUSED and soft-deleted records. */
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
                val monthlyOwn = Normalization.monthlyEquivalent(s.cost.toBigDecimal(), s.billingCycle, s.customDays)
                val monthlyBase = if (rates.knows(s.cost.currency))
                    rates.convert(monthlyOwn, s.cost.currency, base) else monthlyOwn
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
}
