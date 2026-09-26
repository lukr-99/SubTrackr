package com.lukr99.subtrackr.ui.backup

import com.lukr99.subtrackr.domain.backup.RestoreMode

/** Stable test tags for the Backup card and the restore dialogs. */
object BackupTags {
    const val EXPORT = "backup_export"
    const val RESTORE = "backup_restore"
    const val MESSAGE = "backup_message"
    const val CONFIRM = "restore_confirm"
    const val CANCEL = "restore_cancel"
    const val REPLACE_CONFIRM = "restore_replace_confirm"

    fun mode(mode: RestoreMode): String = "restore_mode_${mode.name.lowercase()}"
}
