package com.lukr99.subtrackr.application

import com.lukr99.subtrackr.domain.currency.OfflineFallback
import com.lukr99.subtrackr.model.Database
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.Settings
import com.lukr99.subtrackr.model.Subscription
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test
import java.time.Clock
import java.time.Instant
import java.time.ZoneOffset

class AppRepositoryTest {
    private val clock = Clock.fixed(Instant.parse("2026-09-26T10:00:00Z"), ZoneOffset.UTC)
    private val store = InMemoryDatabaseStore(Database(settings = Settings(baseCurrency = "CZK")))

    private fun repository() = AppRepository(
        store = store,
        rateSource = { anchor -> OfflineFallback.forAnchor(anchor) },
        clock = clock,
        newId = { "11111111-1111-4111-8111-111111111111" },
    )

    @Test
    fun upsert_newSubscription_getsIdAndTimestampsAndIsSaved() {
        val repo = repository()

        repo.upsert(Subscription(name = "Alpha", cost = Money("EUR", 999, 2)))

        val saved = store.saved.subscriptions.single()
        assertEquals("11111111-1111-4111-8111-111111111111", saved.id)
        assertEquals("2026-09-26T10:00:00Z", saved.createdAt)
        assertEquals("2026-09-26T10:00:00Z", saved.updatedAt)
        assertEquals(store.saved, repo.db)
    }

    @Test
    fun delete_existingSubscription_leavesSavedTombstone() {
        store.saved = store.saved.copy(
            subscriptions = listOf(Subscription(id = "a", name = "Alpha", updatedAt = "2026-09-01T10:00:00Z")),
        )
        val repo = repository()

        repo.delete("a")

        val tombstone = store.saved.subscriptions.single()
        assertEquals("2026-09-26T10:00:00Z", tombstone.deletedAt)
        assertEquals("2026-09-26T10:00:00Z", tombstone.updatedAt)
    }

    @Test
    fun today_comesFromTheInjectedClock() {
        assertEquals(java.time.LocalDate.of(2026, 9, 26), repository().today())
    }

    @Test
    fun upsert_whenSaveFails_keepsPreviousState() {
        val repo = repository()
        store.failSaves = true

        assertThrows(java.io.IOException::class.java) { repo.upsert(Subscription(name = "Alpha")) }

        assertEquals(emptyList<Subscription>(), repo.db.subscriptions)
    }
}
