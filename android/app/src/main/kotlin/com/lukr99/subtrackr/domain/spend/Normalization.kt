package com.lukr99.subtrackr.domain.spend

import com.lukr99.subtrackr.model.BillingCycle
import java.math.BigDecimal
import java.math.MathContext

/**
 * Billing-cycle math from contracts/SPEC.md section 2. Exact BigDecimal; callers round only for
 * display. Verified by contracts/vectors/monthly-normalization.json, the file the desktop app runs.
 */
object Normalization {
    /** 365.2425 / 12, the average number of calendar days in a month. */
    val AVG_DAYS_PER_MONTH: BigDecimal = BigDecimal("30.436875")

    private val TWELVE = BigDecimal(12)
    private val FIFTY_TWO = BigDecimal(52)

    fun monthlyEquivalent(cost: BigDecimal, cycle: BillingCycle, customDays: Int = 0): BigDecimal =
        when (cycle) {
            BillingCycle.WEEKLY -> cost.multiply(FIFTY_TWO).divide(TWELVE, MathContext.DECIMAL64)
            BillingCycle.MONTHLY -> cost
            BillingCycle.QUARTERLY -> cost.divide(BigDecimal(3), MathContext.DECIMAL64)
            BillingCycle.SEMIANNUAL -> cost.divide(BigDecimal(6), MathContext.DECIMAL64)
            BillingCycle.ANNUAL -> cost.divide(TWELVE, MathContext.DECIMAL64)
            BillingCycle.CUSTOM_DAYS -> {
                require(customDays > 0) { "CUSTOM_DAYS requires customDays > 0." }
                cost.multiply(AVG_DAYS_PER_MONTH).divide(BigDecimal(customDays), MathContext.DECIMAL64)
            }
            BillingCycle.BILLING_CYCLE_UNSPECIFIED ->
                throw IllegalArgumentException("Unspecified billing cycle.")
        }

    fun yearlyEquivalent(cost: BigDecimal, cycle: BillingCycle, customDays: Int = 0): BigDecimal =
        monthlyEquivalent(cost, cycle, customDays).multiply(TWELVE)
}
