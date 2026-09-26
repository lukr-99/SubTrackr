package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.Database

/** The database a restore produces and how each backup subscription was counted. */
data class RestoreOutcome(
    val database: Database,
    val added: Int,
    val updated: Int,
    val unchanged: Int,
    /** Subscriptions after the restore, tombstones included. */
    val total: Int,
)
