package com.lukr99.subtrackr.application.backup

import java.io.IOException

/** Documents held in memory by URI; [failWrites] and a missing URI behave like I/O errors. */
class FakeDocuments(val files: MutableMap<String, String> = mutableMapOf()) : DocumentGateway {
    var failWrites = false

    override suspend fun readText(uri: String, maxBytes: Int): String? {
        val text = files[uri] ?: throw IOException("no such document")
        return if (text.toByteArray().size > maxBytes) null else text
    }

    override suspend fun writeText(uri: String, text: String) {
        if (failWrites) throw IOException("read-only")
        files[uri] = text
    }
}
