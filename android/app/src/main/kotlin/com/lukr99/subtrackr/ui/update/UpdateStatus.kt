package com.lukr99.subtrackr.ui.update

/** What the updater is doing, shown as one line in Settings. */
sealed interface UpdateStatus {
    data object Idle : UpdateStatus

    /** A development build; checks never run. */
    data object Disabled : UpdateStatus

    data object Checking : UpdateStatus

    data object UpToDate : UpdateStatus

    data class Available(val version: String) : UpdateStatus

    data class Downloading(val version: String) : UpdateStatus

    data object OpeningInstaller : UpdateStatus

    data class CheckFailed(val reason: String) : UpdateStatus

    data class DownloadFailed(val reason: String) : UpdateStatus
}
