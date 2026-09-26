package com.lukr99.subtrackr.application.sync

import java.time.Instant

/** The one sync state the app shows (SPEC.md section 8.4). */
sealed interface SyncStatus {
    /** No project URL and key are configured. */
    data object Off : SyncStatus

    data object SignedOut : SyncStatus

    data object Syncing : SyncStatus

    data class Synced(val at: Instant) : SyncStatus

    data class Failed(val reason: String) : SyncStatus
}
