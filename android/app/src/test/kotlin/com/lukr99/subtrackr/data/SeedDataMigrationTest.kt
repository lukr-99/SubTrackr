package com.lukr99.subtrackr.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.UUID

class SeedDataMigrationTest {

    @Test
    fun legacy_random_seed_ids_become_tombstones_and_stable_live_rows() {
        val stable = SeedData.createInitialDatabase()
        val legacy = stable.copy(subscriptions = stable.subscriptions.flatMap { seed ->
            listOf(
                seed.copy(id = UUID.randomUUID().toString()),
                seed.copy(id = UUID.randomUUID().toString()),
            )
        })

        val migrated = SeedData.migrateLegacyIds(legacy, "2026-08-29T22:00:00Z")
        val live = migrated.subscriptions.filter { it.deletedAt.isBlank() }
        val tombstones = migrated.subscriptions.filter { it.deletedAt.isNotBlank() }

        assertEquals(stable.subscriptions.map { it.id }.sorted(), live.map { it.id }.sorted())
        assertEquals(16, tombstones.size)
        assertTrue(tombstones.all { it.deletedAt == "2026-08-29T22:00:00Z" })
        assertEquals(migrated, SeedData.migrateLegacyIds(migrated, "2026-08-29T23:00:00Z"))
        assertFalse(live.isEmpty())
    }
}
