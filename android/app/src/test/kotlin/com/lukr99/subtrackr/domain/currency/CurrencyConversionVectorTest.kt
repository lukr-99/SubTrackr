package com.lukr99.subtrackr.domain.currency

import com.lukr99.subtrackr.readVector
import com.lukr99.subtrackr.vectorJson
import kotlinx.serialization.Serializable
import org.junit.Assert.assertEquals
import org.junit.Test
import java.math.BigDecimal
import java.math.RoundingMode

class CurrencyConversionVectorTest {

    @Serializable
    data class File(
        val comparePrecision: Int,
        val anchor: String,
        val ratesPerAnchor: Map<String, String>,
        val cases: List<Case>,
    )

    @Serializable
    data class Case(val id: String, val amount: String, val from: String, val to: String, val expected: String)

    @Test
    fun conversion_matches_vectors() {
        val doc = vectorJson.decodeFromString<File>(readVector("currency-conversion.json"))
        val table = ExchangeRateTable(
            doc.anchor,
            doc.ratesPerAnchor.mapValues { BigDecimal(it.value) },
            "test",
        )
        for (c in doc.cases) {
            val actual = table.convert(BigDecimal(c.amount), c.from, c.to)
                .setScale(doc.comparePrecision, RoundingMode.HALF_UP)
            val expected = BigDecimal(c.expected).setScale(doc.comparePrecision, RoundingMode.HALF_UP)
            assertEquals("case ${c.id}", 0, expected.compareTo(actual))
        }
    }
}
