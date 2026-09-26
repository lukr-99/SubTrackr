package com.lukr99.subtrackr.domain.spend

import java.math.BigDecimal

/** Portfolio totals from SPEC.md section 4, in full precision. */
data class SpendSummary(
    val baseCurrency: String,
    val monthlyBase: BigDecimal,
    val yearlyBase: BigDecimal,
    val perSub: List<SubscriptionSpend>,
    val perCurrency: List<CurrencySubtotal>,
    val byCategory: List<CategorySlice>,
)
