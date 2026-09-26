package com.lukr99.subtrackr.application.backup

import com.lukr99.subtrackr.domain.backup.BackupError

/** Outcome of a restore. Only [Restored] changed anything on the device. */
sealed interface RestoreReport {
    data class Restored(val added: Int, val updated: Int, val unchanged: Int, val total: Int) : RestoreReport

    /** The file failed validation; the device's data is untouched. */
    data class Rejected(val error: BackupError) : RestoreReport

    /** Reading the file or saving the result failed; the device's data is untouched. */
    data class Failed(val reason: String) : RestoreReport
}
