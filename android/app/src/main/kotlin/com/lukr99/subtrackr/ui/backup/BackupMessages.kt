package com.lukr99.subtrackr.ui.backup

import com.lukr99.subtrackr.application.backup.ExportReport
import com.lukr99.subtrackr.application.backup.RestoreReport
import com.lukr99.subtrackr.domain.backup.BackupError

/** Backup results as plain sentences for the Backup card. */
object BackupMessages {
    private const val UNCHANGED = "Nothing was changed."

    fun export(report: ExportReport): String = when (report) {
        is ExportReport.Saved ->
            "Backup saved with ${report.subscriptions} subscription${if (report.subscriptions == 1) "" else "s"}."
        is ExportReport.Failed -> "Couldn't save the backup: ${report.reason}."
    }

    fun restore(report: RestoreReport): String = when (report) {
        is RestoreReport.Restored ->
            "Restored: ${report.added} added, ${report.updated} updated, ${report.unchanged} unchanged, " +
                "${report.total} in total."
        is RestoreReport.Failed -> "Restore failed: ${report.reason}. $UNCHANGED"
        is RestoreReport.Rejected -> when (report.error) {
            BackupError.INVALID_JSON ->
                "That file isn't a SubTrackr backup: it isn't valid JSON or it is larger than 10 MB. $UNCHANGED"
            BackupError.UNSUPPORTED_FORMAT -> "That file isn't a SubTrackr backup. $UNCHANGED"
            BackupError.UNSUPPORTED_VERSION ->
                "This backup comes from a newer or unknown version of SubTrackr. Update the app and try again. $UNCHANGED"
            BackupError.INVALID_RECORD ->
                "The backup has a damaged or incomplete subscription, so nothing was restored."
        }
    }
}
