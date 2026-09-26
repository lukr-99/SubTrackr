package com.lukr99.subtrackr.application.sync

/** Sync status plus who is signed in; [email] is null when nobody is. */
data class SyncState(val status: SyncStatus, val email: String? = null)
