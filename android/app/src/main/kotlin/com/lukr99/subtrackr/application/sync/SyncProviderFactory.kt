package com.lukr99.subtrackr.application.sync

/** Builds the transport for the configured project; the composition root picks the adapter. */
fun interface SyncProviderFactory {
    fun create(projectUrl: String, publishableKey: String): SyncProvider
}
