package com.lukr99.subtrackr.ui.sync

import com.lukr99.subtrackr.application.sync.SyncStatus

/** Everything the Sync card shows. */
data class SyncUiState(
    val status: SyncStatus = SyncStatus.Off,
    /** One line: off, signed out, syncing, synced at HH:mm, or failed with a short reason. */
    val statusText: String = "",
    val signedInEmail: String? = null,
    val savedUrl: String = "",
    val savedKey: String = "",
    val url: String = "",
    val key: String = "",
    val email: String = "",
    val code: String = "",
    val step: SignInStep = SignInStep.EMAIL,
    val busy: Boolean = false,
    val error: SyncFormError? = null,
    val errorDetail: String = "",
) {
    val configChanged: Boolean get() = url.trim() != savedUrl || key.trim() != savedKey
    val configured: Boolean get() = status != SyncStatus.Off
    val signedIn: Boolean get() = signedInEmail != null
}
