package com.lukr99.subtrackr.application.backup

import com.lukr99.subtrackr.application.AppRepository
import com.lukr99.subtrackr.application.InMemoryDatabaseStore
import com.lukr99.subtrackr.domain.backup.ProtoJson
import com.lukr99.subtrackr.domain.backup.RestoreMode
import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.readVector
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.int
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import org.junit.Assert.assertEquals
import org.junit.Test
import java.time.Clock
import java.time.Instant
import java.time.ZoneOffset

/** Every case in contracts/vectors/backup.json, through the real parser, restorer, and save. */
class BackupRestoreVectorTest {
    private val cases = Json.parseToJsonElement(readVector("backup.json")).jsonObject.getValue("cases").jsonArray

    private fun service(store: InMemoryDatabaseStore): BackupService {
        val clock = Clock.fixed(Instant.parse("2026-09-26T10:00:00Z"), ZoneOffset.UTC)
        val repository = AppRepository(
            store = store,
            rateSource = { OfflineFallback.forAnchor(it) },
            clock = clock,
            newId = { error("no new records") },
        )
        return BackupService(repository, FakeDocuments(), clock, "0.3.0", work = Dispatchers.Unconfined)
    }

    private fun sameValue(expected: JsonElement, actual: JsonElement?): Boolean {
        val e = expected as JsonPrimitive
        val a = actual as? JsonPrimitive ?: return false
        if (!e.isString && !a.isString) return e.content.toBigDecimal().compareTo(a.content.toBigDecimal()) == 0
        return e.isString == a.isString && e.content == a.content
    }

    @Test
    fun restore_matchesEveryVectorCase() = runTest {
        for (element in cases) {
            val case = element.jsonObject
            val id = case.getValue("id").jsonPrimitive.content
            val mode = RestoreMode.valueOf(case.getValue("mode").jsonPrimitive.content)
            val local = ProtoJson.decodeDatabase(case.getValue("local").jsonObject)
            val text = case["backupText"]?.jsonPrimitive?.content
                ?: Json.encodeToString(JsonObject.serializer(), case.getValue("backup").jsonObject)
            val expected = case.getValue("expected").jsonObject
            val store = InMemoryDatabaseStore(local)

            val report = service(store).restoreText(text, mode)

            if (!expected.getValue("ok").jsonPrimitive.content.toBoolean()) {
                val error = expected.getValue("error").jsonPrimitive.content
                assertEquals("case $id", RestoreReport.Rejected(enumValueOf(error)), report)
                assertEquals("case $id leaves the device unchanged", local, store.saved)
                assertEquals("case $id does not save", 0, store.saveCount)
                continue
            }

            assertEquals(
                "case $id counts",
                RestoreReport.Restored(
                    added = expected.getValue("added").jsonPrimitive.int,
                    updated = expected.getValue("updated").jsonPrimitive.int,
                    unchanged = expected.getValue("unchanged").jsonPrimitive.int,
                    total = expected.getValue("total").jsonPrimitive.int,
                ),
                report,
            )
            val expectedSubs = expected.getValue("subscriptions").jsonArray.map {
                val o = it.jsonObject
                Triple(
                    o.getValue("id").jsonPrimitive.content,
                    o.getValue("updatedAt").jsonPrimitive.content,
                    o.getValue("deletedAt").jsonPrimitive.content,
                )
            }
            val actualSubs = store.saved.subscriptions.sortedBy { it.id }.map { Triple(it.id, it.updatedAt, it.deletedAt) }
            assertEquals("case $id subscriptions", expectedSubs, actualSubs)

            val settings = ProtoJson.encodeSettings(store.saved.settings)
            expected["settings"]?.jsonObject?.forEach { (key, value) ->
                assertEquals("case $id setting $key: ${settings[key]}", true, sameValue(value, settings[key]))
            }
        }
    }
}
