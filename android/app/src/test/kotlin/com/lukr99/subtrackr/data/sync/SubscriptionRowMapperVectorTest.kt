package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.domain.backup.ProtoJson
import com.lukr99.subtrackr.readVector
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/** contracts/vectors/sync-rows.json: toRow for "both" cases, fromRow for every case. */
class SubscriptionRowMapperVectorTest {
    private val doc = Json.parseToJsonElement(readVector("sync-rows.json")).jsonObject
    private val userId = doc.getValue("userId").jsonPrimitive.content

    /** Numbers compare by value (8 equals 8.0); everything else exactly. */
    private fun sameJson(expected: JsonElement, actual: JsonElement?): Boolean {
        val e = expected as? JsonPrimitive ?: return expected == actual
        val a = actual as? JsonPrimitive ?: return false
        if (!e.isString && !a.isString) {
            val en = e.content.toBigDecimalOrNull()
            val an = a.content.toBigDecimalOrNull()
            if (en != null && an != null) return en.compareTo(an) == 0
        }
        return e.isString == a.isString && e.content == a.content
    }

    @Test
    fun rows_matchEveryVectorCase() {
        for (element in doc.getValue("cases").jsonArray) {
            val case = element.jsonObject
            val id = case.getValue("id").jsonPrimitive.content
            val row = case.getValue("row").jsonObject
            val subscription = ProtoJson.decodeSubscription(case.getValue("subscription").jsonObject)

            assertEquals("case $id fromRow", subscription, SubscriptionRowMapper.fromRow(row))

            if (case.getValue("direction").jsonPrimitive.content == "both") {
                val actual: JsonObject = SubscriptionRowMapper.toRow(subscription, userId)
                assertEquals("case $id columns", row.keys, actual.keys)
                for ((column, value) in row) {
                    assertTrue("case $id column $column: ${actual[column]}", sameJson(value, actual[column]))
                }
            }
        }
    }
}
