package com.lukr99.subtrackr.data.backup

import android.content.ContentResolver
import android.net.Uri
import com.lukr99.subtrackr.application.backup.DocumentGateway
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.ByteArrayOutputStream
import java.io.IOException

/** Storage Access Framework documents through the app's ContentResolver. */
class ContentResolverDocuments(private val resolver: ContentResolver) : DocumentGateway {

    override suspend fun readText(uri: String, maxBytes: Int): String? = withContext(Dispatchers.IO) {
        val input = resolver.openInputStream(Uri.parse(uri)) ?: throw IOException("cannot open $uri")
        input.use { stream ->
            val out = ByteArrayOutputStream()
            val buffer = ByteArray(BUFFER_SIZE)
            while (true) {
                val read = stream.read(buffer)
                if (read < 0) break
                out.write(buffer, 0, read)
                // Stop at the limit instead of loading an arbitrarily large file into memory.
                if (out.size() > maxBytes) return@withContext null
            }
            out.toString(Charsets.UTF_8.name())
        }
    }

    override suspend fun writeText(uri: String, text: String) = withContext(Dispatchers.IO) {
        // "wt" truncates, so a shorter backup never leaves the tail of an older one behind.
        val output = resolver.openOutputStream(Uri.parse(uri), "wt") ?: throw IOException("cannot open $uri")
        output.use { it.write(text.toByteArray(Charsets.UTF_8)) }
    }

    private companion object {
        const val BUFFER_SIZE = 64 * 1024
    }
}
