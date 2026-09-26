package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.Database
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.time.Instant
import java.time.temporal.ChronoUnit

/**
 * Writes a full-fidelity backup (SPEC.md section 9.1): every subscription including tombstones and
 * every setting except the device's sync URL and key, which are written empty.
 */
object BackupWriter {
    private val json = Json { prettyPrint = true }

    fun write(database: Database, exportedAt: Instant, appVersion: String): String {
        val exported = database.copy(settings = database.settings.copy(syncUrl = "", syncKey = ""))
        val root = buildJsonObject {
            put("format", BackupFormat.NAME)
            put("formatVersion", BackupFormat.VERSION)
            put("exportedAt", exportedAt.truncatedTo(ChronoUnit.SECONDS).toString())
            put("appVersion", appVersion)
            put("platform", BackupFormat.PLATFORM)
            put("database", ProtoJson.encodeDatabase(exported))
        }
        return json.encodeToString(JsonObject.serializer(), root) + "\n"
    }
}
