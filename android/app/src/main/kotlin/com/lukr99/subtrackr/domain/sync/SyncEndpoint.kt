package com.lukr99.subtrackr.domain.sync

import java.net.URI

/**
 * The Supabase project sync talks to: its URL and publishable key. The key only identifies the
 * project; every request also carries the signed-in user's token (SPEC.md section 8.2).
 */
class SyncEndpoint private constructor(val url: String, val publishableKey: String) {

    override fun equals(other: Any?): Boolean =
        other is SyncEndpoint && other.url == url && other.publishableKey == publishableKey

    override fun hashCode(): Int = 31 * url.hashCode() + publishableKey.hashCode()

    override fun toString(): String = "SyncEndpoint($url)"

    companion object {
        /** Hosts that reach a Supabase stack on the development machine from a device or emulator. */
        private val LOCAL_HOSTS = setOf("10.0.2.2", "127.0.0.1")

        /**
         * A usable endpoint, or null when [url] is not an absolute `https://` URL or [key] is blank.
         * With [allowLocalHttp] (debug builds only), plain `http://` to a local host is accepted too.
         */
        fun parse(url: String, key: String, allowLocalHttp: Boolean = false): SyncEndpoint? {
            val trimmedKey = key.trim()
            if (trimmedKey.isEmpty()) return null
            val uri = runCatching { URI(url.trim()) }.getOrNull() ?: return null
            val host = uri.host ?: return null
            val allowed = when (uri.scheme?.lowercase()) {
                "https" -> true
                "http" -> allowLocalHttp && host in LOCAL_HOSTS
                else -> false
            }
            if (!allowed || uri.rawQuery != null || uri.rawFragment != null) return null
            return SyncEndpoint(url.trim().trimEnd('/'), trimmedKey)
        }
    }
}
