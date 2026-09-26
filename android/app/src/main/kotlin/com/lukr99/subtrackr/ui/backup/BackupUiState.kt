package com.lukr99.subtrackr.ui.backup

import com.lukr99.subtrackr.domain.backup.RestoreMode

/** Everything the Backup card and its restore dialogs show. */
data class BackupUiState(
    val busy: Boolean = false,
    /** The last result in plain words, empty before the first export or restore. */
    val message: String = "",
    val messageIsError: Boolean = false,
    /** The picked backup file while the user chooses how to restore it. */
    val pendingUri: String? = null,
    val mode: RestoreMode = RestoreMode.MERGE,
    /** Replace asks a second time before anything changes. */
    val confirmingReplace: Boolean = false,
)
