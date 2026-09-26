package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.application.sync.AuthSession
import com.lukr99.subtrackr.application.sync.SessionStore
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.put
import java.io.File
import java.io.FileOutputStream
import java.nio.file.AtomicMoveNotSupportedException
import java.nio.file.Files
import java.nio.file.StandardCopyOption
import java.time.Instant

/**
 * Keeps the sign-in session in one encrypted file. The app puts it in `noBackupFilesDir`, apart
 * from data.json, so it never reaches Android backups or SubTrackr backup files. A file that no
 * longer decrypts (for example after a reinstall) is deleted and reads as signed out.
 */
class EncryptedSessionStore(private val file: File, private val cipher: SessionCipher) : SessionStore {

    @Synchronized
    override fun load(): AuthSession? {
        if (!file.exists()) return null
        return try {
            val root = Json.parseToJsonElement(String(cipher.decrypt(file.readBytes()), Charsets.UTF_8)).jsonObject
            fun text(key: String) = root.getValue(key).jsonPrimitive.content
            AuthSession(
                accessToken = text("accessToken"),
                refreshToken = text("refreshToken"),
                expiresAt = Instant.ofEpochSecond(text("expiresAt").toLong()),
                userId = text("userId"),
                email = text("email"),
            )
        } catch (_: Exception) {
            file.delete()
            null
        }
    }

    @Synchronized
    override fun save(session: AuthSession) {
        val plain = buildJsonObject {
            put("accessToken", session.accessToken)
            put("refreshToken", session.refreshToken)
            put("expiresAt", session.expiresAt.epochSecond)
            put("userId", session.userId)
            put("email", session.email)
        }.toString().toByteArray(Charsets.UTF_8)
        val directory = file.absoluteFile.parentFile
        directory?.mkdirs()
        val tmp = File(directory, file.name + ".tmp")
        FileOutputStream(tmp).use { out ->
            out.write(cipher.encrypt(plain))
            out.fd.sync()
        }
        try {
            Files.move(tmp.toPath(), file.toPath(), StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING)
        } catch (_: AtomicMoveNotSupportedException) {
            Files.move(tmp.toPath(), file.toPath(), StandardCopyOption.REPLACE_EXISTING)
        }
    }

    @Synchronized
    override fun clear() {
        file.delete()
    }
}
