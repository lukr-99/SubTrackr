package com.lukr99.subtrackr.application.backup

/**
 * Reads and writes documents the user picked (Storage Access Framework on Android). URIs stay
 * plain strings so this layer has no platform types. Implementations throw IOException on failure.
 */
interface DocumentGateway {
    /** The document as UTF-8 text, or null when it is larger than [maxBytes]. */
    suspend fun readText(uri: String, maxBytes: Int): String?

    /** Replaces the document's content with [text]. */
    suspend fun writeText(uri: String, text: String)
}
