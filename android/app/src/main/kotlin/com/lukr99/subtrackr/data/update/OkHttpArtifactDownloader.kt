package com.lukr99.subtrackr.data.update

import com.lukr99.subtrackr.application.update.ArtifactDownloader
import com.lukr99.subtrackr.data.http.sendExpectingSuccess
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ensureActive
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.io.IOException

/**
 * Downloads release assets. The client passed in must not follow HTTPS-to-HTTP redirects, so an
 * asset URL that ReleaseSelector accepted as HTTPS stays HTTPS.
 */
class OkHttpArtifactDownloader(private val client: OkHttpClient) : ArtifactDownloader {

    override suspend fun readText(url: String): String =
        client.sendExpectingSuccess(request(url)).use { response ->
            withContext(Dispatchers.IO) {
                val source = (response.body ?: throw IOException("empty response")).source()
                // A checksum file is a line of text; refuse anything much larger.
                if (source.request(MAX_TEXT_BYTES + 1)) throw IOException("text asset too large")
                source.buffer.readUtf8()
            }
        }

    override suspend fun download(url: String, target: File) {
        client.sendExpectingSuccess(request(url)).use { response ->
            withContext(Dispatchers.IO) {
                val body = response.body ?: throw IOException("empty response")
                target.parentFile?.mkdirs()
                body.byteStream().use { input ->
                    target.outputStream().use { output ->
                        val buffer = ByteArray(BUFFER_SIZE)
                        while (true) {
                            ensureActive()
                            val read = input.read(buffer)
                            if (read < 0) break
                            output.write(buffer, 0, read)
                        }
                    }
                }
            }
        }
    }

    private fun request(url: String): Request = Request.Builder()
        .url(url)
        .header("Accept", "application/octet-stream")
        .header("User-Agent", "SubTrackr-Android-updater")
        .build()

    private companion object {
        const val BUFFER_SIZE = 64 * 1024
        const val MAX_TEXT_BYTES = 4L * 1024
    }
}
