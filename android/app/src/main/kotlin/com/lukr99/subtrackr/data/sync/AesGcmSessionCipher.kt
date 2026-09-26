package com.lukr99.subtrackr.data.sync

import java.security.GeneralSecurityException
import javax.crypto.Cipher
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/**
 * AES-256-GCM with a fresh IV per write. Output: one format byte, the 12-byte IV, then the
 * ciphertext with its 16-byte tag. [key] supplies the key: an Android Keystore key in the app, a
 * software key in tests.
 */
class AesGcmSessionCipher(private val key: () -> SecretKey) : SessionCipher {

    override fun encrypt(plain: ByteArray): ByteArray {
        val cipher = Cipher.getInstance(TRANSFORMATION)
        // The Keystore generates the IV itself; asking for our own would be refused.
        cipher.init(Cipher.ENCRYPT_MODE, key())
        val iv = cipher.iv
        check(iv.size == IV_BYTES) { "unexpected IV length ${iv.size}" }
        return byteArrayOf(FORMAT) + iv + cipher.doFinal(plain)
    }

    override fun decrypt(sealed: ByteArray): ByteArray {
        if (sealed.size < 1 + IV_BYTES + TAG_BYTES || sealed[0] != FORMAT) {
            throw GeneralSecurityException("not a session file")
        }
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.DECRYPT_MODE, key(), GCMParameterSpec(TAG_BYTES * 8, sealed, 1, IV_BYTES))
        return cipher.doFinal(sealed, 1 + IV_BYTES, sealed.size - 1 - IV_BYTES)
    }

    private companion object {
        const val TRANSFORMATION = "AES/GCM/NoPadding"
        const val FORMAT: Byte = 1
        const val IV_BYTES = 12
        const val TAG_BYTES = 16
    }
}
