package com.lukr99.subtrackr.domain.backup

/** Constants of the backup file format (SPEC.md section 9.1). */
object BackupFormat {
    const val NAME = "subtrackr-backup"
    const val VERSION = 1
    const val PLATFORM = "android"

    /** Larger files are refused before parsing. */
    const val MAX_BYTES = 10 * 1024 * 1024
}
