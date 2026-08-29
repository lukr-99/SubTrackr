package com.lukr99.subtrackr.data

import com.lukr99.subtrackr.model.Database
import kotlinx.serialization.json.Json
import java.io.File

/**
 * Loads/saves the whole [Database] as one JSON file (mirrors the WPF DataStore). JSON field names
 * match the proto so the format stays sync-compatible. Atomic writes (temp + rename).
 */
class SubscriptionStore(private val file: File) {

    private val json = Json {
        prettyPrint = true
        ignoreUnknownKeys = true
        encodeDefaults = true
    }

    fun load(): Database {
        if (!file.exists()) {
            val seeded = SeedData.createInitialDatabase()
            save(seeded)
            return seeded
        }
        val loaded = runCatching { json.decodeFromString<Database>(file.readText()) }
            .getOrElse { SeedData.createInitialDatabase() }
        val migrated = SeedData.migrateLegacyIds(loaded)
        if (migrated != loaded) save(migrated)
        return migrated
    }

    fun save(db: Database) {
        file.parentFile?.mkdirs()
        val tmp = File(file.parentFile, file.name + ".tmp")
        tmp.writeText(json.encodeToString(Database.serializer(), db))
        if (file.exists()) file.delete()
        tmp.renameTo(file)
    }
}
