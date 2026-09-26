package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.Database
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive

/**
 * Parses and validates backup text (SPEC.md section 9.2) before anything changes. Checks run in the
 * spec's order and stop at the first failure. Verified by contracts/vectors/backup.json.
 */
object BackupReader {
    private val UUID = Regex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")
    private val CURRENCY = Regex("^[A-Z]{3}$")
    private val EXPONENTS = 0..4

    fun read(text: String): BackupReadResult {
        if (utf8Length(text) > BackupFormat.MAX_BYTES) return invalid(BackupError.INVALID_JSON)
        val root = try {
            Json.parseToJsonElement(text) as? JsonObject
        } catch (_: SerializationException) {
            null
        } catch (_: IllegalArgumentException) {
            null
        } ?: return invalid(BackupError.INVALID_JSON)

        val format = root["format"] as? JsonPrimitive
        if (format == null || !format.isString || format.content != BackupFormat.NAME) {
            return invalid(BackupError.UNSUPPORTED_FORMAT)
        }
        if (!isSupportedVersion(root["formatVersion"])) return invalid(BackupError.UNSUPPORTED_VERSION)

        val databaseJson = root["database"] as? JsonObject ?: return invalid(BackupError.INVALID_RECORD)
        val database = try {
            ProtoJson.decodeDatabase(databaseJson)
        } catch (_: ProtoJsonException) {
            return invalid(BackupError.INVALID_RECORD)
        }
        if (!recordsAreValid(database)) return invalid(BackupError.INVALID_RECORD)

        return BackupReadResult.Valid(
            BackupFile(
                exportedAt = root.text("exportedAt"),
                appVersion = root.text("appVersion"),
                platform = root.text("platform"),
                database = database,
            ),
        )
    }

    private fun invalid(error: BackupError) = BackupReadResult.Invalid(error)

    /** `formatVersion` must be a JSON number (not text) with an integer value of exactly 1. */
    private fun isSupportedVersion(value: Any?): Boolean {
        val primitive = value as? JsonPrimitive ?: return false
        if (primitive.isString) return false
        val number = primitive.content.toBigDecimalOrNull() ?: return false
        if (number.stripTrailingZeros().scale() > 0) return false
        return number.compareTo(BackupFormat.VERSION.toBigDecimal()) == 0
    }

    private fun recordsAreValid(database: Database): Boolean {
        val seen = HashSet<String>()
        return database.subscriptions.all { s ->
            UUID.matches(s.id) &&
                seen.add(s.id.lowercase()) &&
                CURRENCY.matches(s.cost.currency) &&
                s.cost.exponent in EXPONENTS &&
                s.updatedAt.isNotEmpty()
        }
    }

    private fun JsonObject.text(key: String): String =
        (this[key] as? JsonPrimitive)?.takeIf { it.isString }?.content.orEmpty()

    private fun utf8Length(text: String): Long {
        var bytes = 0L
        var i = 0
        while (i < text.length) {
            val c = text[i]
            bytes += when {
                c.code < 0x80 -> 1
                c.code < 0x800 -> 2
                Character.isHighSurrogate(c) && i + 1 < text.length && Character.isLowSurrogate(text[i + 1]) -> {
                    i++
                    4
                }
                else -> 3
            }
            i++
        }
        return bytes
    }
}
