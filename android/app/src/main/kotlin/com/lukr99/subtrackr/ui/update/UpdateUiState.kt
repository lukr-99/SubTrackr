package com.lukr99.subtrackr.ui.update

import com.lukr99.subtrackr.domain.update.UpdateOffer

/** Everything the Updates card and the update prompt show. */
data class UpdateUiState(
    val currentVersion: String,
    val checksEnabled: Boolean,
    val status: UpdateStatus = if (checksEnabled) UpdateStatus.Idle else UpdateStatus.Disabled,
    /** The newest release on offer, kept after the prompt is dismissed. */
    val offer: UpdateOffer? = null,
    val promptVisible: Boolean = false,
    val busy: Boolean = false,
) {
    /** The status as a sentence for the Settings card. */
    val statusText: String
        get() = when (val s = status) {
            UpdateStatus.Idle -> "Installed version: v$currentVersion"
            UpdateStatus.Disabled -> "Version $currentVersion. Development builds don't check for updates."
            UpdateStatus.Checking -> "Checking for updates…"
            UpdateStatus.UpToDate -> "You're on the latest version (v$currentVersion)."
            is UpdateStatus.Available -> "Version ${s.version} is available."
            is UpdateStatus.Downloading -> "Downloading version ${s.version}…"
            UpdateStatus.OpeningInstaller -> "Opening the installer…"
            is UpdateStatus.CheckFailed -> "Couldn't check for updates: ${s.reason}."
            is UpdateStatus.DownloadFailed -> "Update failed: ${s.reason}."
        }
}
