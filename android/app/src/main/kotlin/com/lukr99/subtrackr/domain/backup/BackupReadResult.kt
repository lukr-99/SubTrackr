package com.lukr99.subtrackr.domain.backup

/** Result of reading backup text: a valid file, or the first validation failure. */
sealed interface BackupReadResult {
    data class Valid(val backup: BackupFile) : BackupReadResult

    data class Invalid(val error: BackupError) : BackupReadResult
}
