package com.lukr99.subtrackr

import com.lukr99.subtrackr.domain.WorthIt
import kotlinx.serialization.Serializable
import org.junit.Assert.assertEquals
import org.junit.Test
import java.math.BigDecimal
import java.math.RoundingMode

class WorthItVectorTest {

    @Serializable
    data class File(val comparePrecision: Int, val cases: List<Case>)

    @Serializable
    data class Case(
        val id: String, val monthlyBase: String, val usesPerMonth: Double,
        val threshold: String, val expectedCostPerUse: String, val expectedVerdict: String,
    )

    @Test
    fun worthIt_matches_vectors() {
        val doc = vectorJson.decodeFromString<File>(readVector("worth-it.json"))
        for (c in doc.cases) {
            val monthly = BigDecimal(c.monthlyBase)
            val cpu = WorthIt.costPerUse(monthly, c.usesPerMonth)
                .setScale(doc.comparePrecision, RoundingMode.HALF_UP)
            val expectedCpu = BigDecimal(c.expectedCostPerUse).setScale(doc.comparePrecision, RoundingMode.HALF_UP)
            assertEquals("cpu ${c.id}", 0, expectedCpu.compareTo(cpu))

            val verdict = WorthIt.evaluate(monthly, c.usesPerMonth, BigDecimal(c.threshold)).name
            assertEquals("verdict ${c.id}", c.expectedVerdict, verdict)
        }
    }
}
