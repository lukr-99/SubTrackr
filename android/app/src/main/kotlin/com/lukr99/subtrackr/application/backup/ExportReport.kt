package com.lukr99.subtrackr.application.backup

/** Outcome of writing a backup file. */
sealed interface ExportReport {
    /** [subscriptions] counts the live subscriptions written; tombstones are written too. */
    data class Saved(val subscriptions: Int) : ExportReport

    data class Failed(val reason: String) : ExportReport
}
