package com.lukr99.subtrackr.domain.spend

import java.math.BigDecimal

/** Active spend in one source currency, before conversion. */
data class CurrencySubtotal(val currency: String, val monthly: BigDecimal, val yearly: BigDecimal)
