package com.lukr99.subtrackr.domain.spend

import java.math.BigDecimal

/** Active spend of one category in the base currency. */
data class CategorySlice(val category: String, val monthlyBase: BigDecimal, val yearlyBase: BigDecimal)
