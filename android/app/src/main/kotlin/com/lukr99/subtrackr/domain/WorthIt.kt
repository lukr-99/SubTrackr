package com.lukr99.subtrackr.domain

import java.math.BigDecimal
import java.math.MathContext

enum class WorthVerdict { UNKNOWN, WORTH, NOT_WORTH }

/** Worth-it / not-worth-it from SPEC.md §5. Verified by contracts/vectors/worth-it.json. */
object WorthIt {
    val DEFAULT_THRESHOLD: BigDecimal = BigDecimal("2.00")

    fun costPerUse(monthlyBase: BigDecimal, usesPerMonth: Double): BigDecimal =
        if (usesPerMonth <= 0.0) BigDecimal.ZERO
        else monthlyBase.divide(BigDecimal(usesPerMonth), MathContext.DECIMAL64)

    fun evaluate(monthlyBase: BigDecimal, usesPerMonth: Double, threshold: BigDecimal): WorthVerdict {
        if (usesPerMonth <= 0.0) return WorthVerdict.UNKNOWN
        return if (costPerUse(monthlyBase, usesPerMonth) <= threshold) WorthVerdict.WORTH
        else WorthVerdict.NOT_WORTH
    }
}
