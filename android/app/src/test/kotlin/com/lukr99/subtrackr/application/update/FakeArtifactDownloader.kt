package com.lukr99.subtrackr.application.update

import java.io.File

/** Serves fixed bytes per URL; a URL mapped to null fails like an unreachable server. */
class FakeArtifactDownloader(private val files: Map<String, ByteArray?>) : ArtifactDownloader {
    val requested = mutableListOf<String>()

    override suspend fun readText(url: String): String {
        requested += url
        return String(files[url] ?: throw java.io.IOException("unreachable"), Charsets.UTF_8)
    }

    override suspend fun download(url: String, target: File) {
        requested += url
        val bytes = files[url] ?: throw java.io.IOException("unreachable")
        target.parentFile?.mkdirs()
        target.writeBytes(bytes)
    }
}
