package com.lukr99.subtrackr.application.update

import java.io.File

/** Fetches release assets. Implementations throw on network or HTTP failure. */
interface ArtifactDownloader {
    /** A small text asset such as a `.sha256` file. */
    suspend fun readText(url: String): String

    /** Streams [url] into [target], replacing it. */
    suspend fun download(url: String, target: File)
}
