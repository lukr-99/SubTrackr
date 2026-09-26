package com.lukr99.subtrackr.domain.backup

import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

/** The suggested file name, `SubTrackr-backup-YYYYMMDD-HHMMSS.json` in local time. */
object BackupFileName {
    private val STAMP = DateTimeFormatter.ofPattern("yyyyMMdd-HHmmss")

    fun at(localTime: LocalDateTime): String = "SubTrackr-backup-${STAMP.format(localTime)}.json"
}
