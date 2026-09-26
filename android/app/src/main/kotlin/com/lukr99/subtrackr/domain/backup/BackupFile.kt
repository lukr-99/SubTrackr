package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.Database

/** A validated backup: its header and the database it carries. */
data class BackupFile(
    val exportedAt: String,
    val appVersion: String,
    val platform: String,
    val database: Database,
)
