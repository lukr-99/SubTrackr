package com.lukr99.subtrackr.data

import com.lukr99.subtrackr.application.store.DatabaseStore
import com.lukr99.subtrackr.model.Database
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import java.io.File
import java.io.FileOutputStream
import java.nio.file.AtomicMoveNotSupportedException
import java.nio.file.Files
import java.nio.file.StandardCopyOption
import java.time.Clock
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

/**
 * Loads and saves the whole [Database] as one JSON file (mirrors the desktop DataStore). JSON field
 * names match the proto so the format stays sync-compatible. A save writes and syncs a temp file,
 * then renames it over data.json, so a crash leaves either the old or the new file, never half.
 * A file that cannot be parsed is renamed to `data.json.unreadable-<UTC time>` and never
 * overwritten, so the app starts fresh without destroying data someone may still recover.
 */
class SubscriptionStore(
    private val file: File,
    private val clock: Clock = Clock.systemUTC(),
) : DatabaseStore {

    /** Where the last unreadable file was moved, or null when every load succeeded. */
    var setAsideFile: File? = null
        private set

    private val json = Json {
        prettyPrint = true
        ignoreUnknownKeys = true
        encodeDefaults = true
    }

    override fun load(): Database {
        if (!file.exists()) {
            val seeded = SeedData.createInitialDatabase()
            save(seeded)
            return seeded
        }
        val loaded = try {
            json.decodeFromString<Database>(file.readText())
        } catch (_: SerializationException) {
            setAside()
            SeedData.createInitialDatabase().also(::save)
        } catch (_: IllegalArgumentException) {
            setAside()
            SeedData.createInitialDatabase().also(::save)
        }
        val migrated = SeedData.migrateLegacyIds(loaded)
        if (migrated != loaded) save(migrated)
        return migrated
    }

    private fun setAside() {
        val stamp = DateTimeFormatter.ofPattern("yyyyMMdd'T'HHmmss'Z'").withZone(ZoneOffset.UTC).format(clock.instant())
        val target = File(file.absoluteFile.parentFile, "${file.name}.unreadable-$stamp")
        Files.move(file.toPath(), target.toPath())
        setAsideFile = target
    }

    override fun save(db: Database) {
        val directory = file.absoluteFile.parentFile
        directory?.mkdirs()
        val tmp = File(directory, file.name + ".tmp")
        FileOutputStream(tmp).use { out ->
            out.write(json.encodeToString(Database.serializer(), db).toByteArray(Charsets.UTF_8))
            out.fd.sync()
        }
        try {
            Files.move(tmp.toPath(), file.toPath(), StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING)
        } catch (_: AtomicMoveNotSupportedException) {
            Files.move(tmp.toPath(), file.toPath(), StandardCopyOption.REPLACE_EXISTING)
        }
    }
}
