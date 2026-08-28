package com.lukr99.subtrackr

import com.lukr99.subtrackr.domain.Normalization
import com.lukr99.subtrackr.model.BillingCycle
import kotlinx.serialization.Serializable
import org.junit.Assert.assertEquals
import org.junit.Test
import java.math.BigDecimal
import java.math.RoundingMode

class MonthlyNormalizationVectorTest {

    @Serializable
    data class File(val comparePrecision: Int, val cases: List<Case>)

    @Serializable
    data class Case(
        val id: String, val cost: String, val currency: String,
        val cycle: String, val customDays: Int, val expectedMonthly: String,
    )

    @Test
    fun monthlyEquivalent_matches_vectors() {
        val doc = vectorJson.decodeFromString<File>(readVector("monthly-normalization.json"))
        for (c in doc.cases) {
            val actual = Normalization
                .monthlyEquivalent(BigDecimal(c.cost), BillingCycle.valueOf(c.cycle), c.customDays)
                .setScale(doc.comparePrecision, RoundingMode.HALF_UP)
            val expected = BigDecimal(c.expectedMonthly).setScale(doc.comparePrecision, RoundingMode.HALF_UP)
            assertEquals("case ${c.id}", 0, expected.compareTo(actual))
        }
    }
}
