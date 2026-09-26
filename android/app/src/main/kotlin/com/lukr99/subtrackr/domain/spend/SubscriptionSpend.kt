package com.lukr99.subtrackr.domain.spend

import com.lukr99.subtrackr.domain.worth.WorthVerdict
import com.lukr99.subtrackr.model.Subscription
import java.math.BigDecimal

/** One subscription with its normalized spend and worth verdict. */
data class SubscriptionSpend(
    val subscription: Subscription,
    val monthlyOwn: BigDecimal,
    val monthlyBase: BigDecimal,
    val yearlyBase: BigDecimal,
    val costPerUse: BigDecimal,
    val verdict: WorthVerdict,
)
