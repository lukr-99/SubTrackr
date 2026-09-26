package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.domain.sync.MergeEngine
import com.lukr99.subtrackr.model.Database

/** Builds the restored database in memory (SPEC.md section 9.3); saving it is the caller's job. */
object BackupRestorer {

    fun restore(local: Database, backup: Database, mode: RestoreMode): RestoreOutcome = when (mode) {
        RestoreMode.MERGE -> merge(local, backup)
        RestoreMode.REPLACE -> replace(local, backup)
    }

    private fun merge(local: Database, backup: Database): RestoreOutcome {
        val merged = MergeEngine.merge(local.subscriptions, backup.subscriptions)
        val localById = local.subscriptions.associateBy { it.id }
        val mergedById = merged.associateBy { it.id }
        var added = 0
        var updated = 0
        var unchanged = 0
        for (fromBackup in backup.subscriptions) {
            val onDevice = localById[fromBackup.id]
            val winner = mergedById.getValue(fromBackup.id)
            when {
                onDevice == null -> added++
                winner.updatedAt != onDevice.updatedAt || winner.deletedAt != onDevice.deletedAt -> updated++
                else -> unchanged++
            }
        }
        return RestoreOutcome(local.copy(subscriptions = merged), added, updated, unchanged, merged.size)
    }

    private fun replace(local: Database, backup: Database): RestoreOutcome {
        val settings = backup.settings.copy(
            schemaVersion = local.settings.schemaVersion,
            syncUrl = local.settings.syncUrl,
            syncKey = local.settings.syncKey,
        )
        val subscriptions = backup.subscriptions
        return RestoreOutcome(
            database = local.copy(settings = settings, subscriptions = subscriptions),
            added = subscriptions.size,
            updated = 0,
            unchanged = 0,
            total = subscriptions.size,
        )
    }
}
