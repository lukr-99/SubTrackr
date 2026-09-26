package com.lukr99.subtrackr.data.sync

import com.lukr99.subtrackr.application.sync.AuthSession
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File
import java.time.Instant
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey

class EncryptedSessionStoreTest {
    @get:Rule
    val temp = TemporaryFolder()

    private fun key(): SecretKey = KeyGenerator.getInstance("AES").apply { init(256) }.generateKey()

    private val session = AuthSession(
        accessToken = "access-token-value",
        refreshToken = "refresh-token-value",
        expiresAt = Instant.parse("2026-09-26T11:00:00Z"),
        userId = "9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b",
        email = "user@example.com",
    )

    @Test
    fun saveThenLoad_roundTripsAndKeepsTokensOutOfTheFile() {
        val file = File(temp.root, "sync-session.bin")
        val key = key()
        val store = EncryptedSessionStore(file, AesGcmSessionCipher { key })

        store.save(session)

        assertEquals(session, EncryptedSessionStore(file, AesGcmSessionCipher { key }).load())
        val bytes = String(file.readBytes(), Charsets.ISO_8859_1)
        assertFalse(bytes.contains("access-token-value"))
        assertFalse(bytes.contains("user@example.com"))
        assertFalse(File(temp.root, "sync-session.bin.tmp").exists())
    }

    @Test
    fun load_withAnotherKey_readsAsSignedOutAndDeletesTheFile() {
        val file = File(temp.root, "sync-session.bin")
        val first = key()
        EncryptedSessionStore(file, AesGcmSessionCipher { first }).save(session)
        val second = key()

        assertNull(EncryptedSessionStore(file, AesGcmSessionCipher { second }).load())
        assertFalse(file.exists())
    }

    @Test
    fun clear_removesTheSession() {
        val file = File(temp.root, "sync-session.bin")
        val key = key()
        val store = EncryptedSessionStore(file, AesGcmSessionCipher { key })
        store.save(session)

        store.clear()

        assertNull(store.load())
    }

    @Test
    fun toString_neverShowsTokens() {
        assertFalse(session.toString().contains("token-value"))
    }
}
