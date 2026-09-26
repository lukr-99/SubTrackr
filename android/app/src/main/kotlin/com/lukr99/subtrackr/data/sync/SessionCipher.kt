package com.lukr99.subtrackr.data.sync

/** Seals the session file. Decrypting tampered or foreign data throws. */
interface SessionCipher {
    fun encrypt(plain: ByteArray): ByteArray

    fun decrypt(sealed: ByteArray): ByteArray
}
